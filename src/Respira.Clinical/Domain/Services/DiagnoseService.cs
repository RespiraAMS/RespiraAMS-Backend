using System.Globalization;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Domain.Services
{

    /// <summary>
    /// Diagnose service
    /// </summary>
    /// <param name="logger">Logger</param>
    public class DiagnoseService(ILogger<DiagnoseService> logger) : IDiagnoseService
    {
        /// <summary>
        /// Render one observation as an indented evidence line. Each value type carries its own
        /// slot on <see cref="ClinicalObservation"/>, so the right one has to be picked - reading
        /// <c>NumericValue</c> for a boolean observation would render an empty value.
        /// </summary>
        private static string FormatObservation(ClinicalObservation observation)
        {
            var value = observation.Variable.ValueType switch
            {
                ClinicalValueType.Numeric => observation.NumericValue?.ToString(CultureInfo.InvariantCulture),
                ClinicalValueType.Boolean => observation.BooleanValue switch
                {
                    true => "true",
                    false => "false",
                    null => null,
                },
                _ => observation.CategoricalValue,
            };

            return $"\t{observation.Variable.Code}: {value ?? "(no value)"}";
        }

        /// <summary>
        /// Render an evidence entry for a criterion. A formula cannot be evaluated when the
        /// observations do not contain every variable it uses, so the evidence reports the
        /// missing variables instead of throwing (the score itself is already handled by
        /// <see cref="Criterion.IsCriterionSastisfied(IEnumerable{ClinicalObservation})"/>).
        /// The entry is multi-line (<c>\n</c> between sections, <c>\t</c> per observation);
        /// clients render it with preserved whitespace.
        /// </summary>
        /// <param name="source">Where the criterion comes from, e.g. metric or pathogen name</param>
        /// <param name="formula">Criterion formula</param>
        /// <param name="observations">Clinical observations</param>
        /// <returns>Evidence string</returns>
        private static string BuildEvidence(string source, Formula formula, IEnumerable<ClinicalObservation> observations)
        {
            var observationList = observations
                .Where(x => formula.Variables.Select(x => x.Code).ToList().Contains(x.Variable.Code))
                .ToList();

            var missing = formula.Variables
                .Where(v => !observationList.Any(o => o.Variable.Code.Equals(v.Code)))
                .Select(v => v.Code)
                .ToList();

            var variables = observationList.Count > 0
                ? string.Join("\n", observationList.Select(FormatObservation))
                : "\t(none)";

            var value = missing.Count > 0
                ? $"not evaluated (missing {string.Join(", ", missing)})"
                : formula.ToExpression(observationList).Evaluate();

            return $"{source}:\nFormula: {formula}\nVariables:\n{variables}\nValue: {value}";
        }

        /// <summary>
        /// Calculate metrics score
        /// </summary>
        /// <param name="metrics">Metrics</param>
        /// <param name="observations">Clinical observations</param>
        /// <returns>Score result</returns>
        public (decimal, List<string>) CalculateMetricsScore(ClinicalMetrics metrics, IEnumerable<ClinicalObservation> observations)
        {
            if (metrics.IsMajorMinorMetric)
            {
                throw new ArgumentException("Cannot calculate score for a Major/Minor metrics system");
            }

            var evidences = new List<string>();
            var totalScore = metrics.Rules.Sum(r =>
            {
                // Type casting to scoring rule
                var rule = (ScoringRule)r;

                // Calculate score
                var score = rule.GetScore(observations);
                logger.LogDebug($"Calculated score for {metrics.Name}/{rule.Criterion.Name}: {score}");

                // Add evidence
                evidences.Add(BuildEvidence($"{metrics.Name} - {rule.Criterion.Name}", rule.Criterion.Formula, observations));

                // Return score
                return score;
            });

            return (totalScore, evidences);
        }

        /// <summary>
        /// CURB-65 severity diagnosis.
        /// </summary>
        /// <param name="score">Score</param>
        /// <returns>Severity diagnosis</returns>
        /// <exception cref="InvalidOperationException">Throw if received invalid score</exception>
        public ScoreMetricsSeverityDiagnosis Curb65(int score, List<string> evidences, bool isBunMissing = false)
        {
            /*
             * CURB-65 (according to our internal definition):
             * 1. If BUN is missing, then this would still be valid (CRB-65)
             * 2. For CURB-65:
             * 2.1. 0 - 1: Mild + Outpatient
             * 2.2. 2: Moderate + Inpatient
             * 2.3. 3: Severe + Inpatient
             * 2.4. 4 - 5: Severe + Intensive Care Unit
             * 3. For CRB-65:
             * 3.1. 0: Mild + Outpatient
             * 3.2. 1 - 2: Moderate + Inpatient
             * 3.3. 3: Severe + Inpatient
             * 3.4. 4: Severe + Intensive Care Unit
             */

            string code = isBunMissing ? "CRB-65" : "CURB-65";

            // If BUN variable is missing (to be more exact, urea), then this would still be valid
            // (CRB-65, which has different value matching)
            if (isBunMissing)
            {
                return score switch
                {
                    < 0 => throw new ArgumentOutOfRangeException(nameof(score), "Score cannot be negative"),
                    0 => new ScoreMetricsSeverityDiagnosis
                    {
                        Code = code,
                        Score = score,
                        Severity = Severity.Mild,
                        TreatmentSite = TreatmentSite.Outpatient,
                        Evidences = evidences,
                    },
                    1 or 2 => new ScoreMetricsSeverityDiagnosis
                    {
                        Code = code,
                        Score = score,
                        Severity = Severity.Moderate,
                        TreatmentSite = TreatmentSite.Inpatient,
                        Evidences = evidences,
                    },
                    3 => new ScoreMetricsSeverityDiagnosis
                    {
                        Code = code,
                        Score = score,
                        Severity = Severity.Severe,
                        TreatmentSite = TreatmentSite.Inpatient,
                        Evidences = evidences,
                    },
                    4 => new ScoreMetricsSeverityDiagnosis
                    {
                        Code = code,
                        Score = score,
                        Severity = Severity.Severe,
                        TreatmentSite = TreatmentSite.IntensiveCareUnit,
                        Evidences = evidences,
                    },
                    _ => throw new InvalidOperationException($"Unexpected CRB-65 score: {score}"),
                }
            ;
            }

            // If BUN is present, then this would be CURB-65
            return score switch
            {
                < 0 => throw new ArgumentOutOfRangeException(nameof(score), "Score cannot be negative"),
                0 or 1 => new ScoreMetricsSeverityDiagnosis
                {
                    Code = code,
                    Score = score,
                    Severity = Severity.Mild,
                    TreatmentSite = TreatmentSite.Outpatient,
                    Evidences = evidences,
                },
                2 => new ScoreMetricsSeverityDiagnosis
                {
                    Code = code,
                    Score = score,
                    Severity = Severity.Moderate,
                    TreatmentSite = TreatmentSite.Inpatient,
                    Evidences = evidences,
                },
                3 => new ScoreMetricsSeverityDiagnosis
                {
                    Code = code,
                    Score = score,
                    Severity = Severity.Severe,
                    TreatmentSite = TreatmentSite.Inpatient,
                    Evidences = evidences,
                },
                4 or 5 => new ScoreMetricsSeverityDiagnosis
                {
                    Code = code,
                    Score = score,
                    Severity = Severity.Severe,
                    TreatmentSite = TreatmentSite.IntensiveCareUnit,
                    Evidences = evidences,
                },
                _ => throw new InvalidOperationException($"Unexpected CURB-65 score: {score}"),
            };
        }

        /// <summary>
        /// PSI severity diagnosis. Note that, if patient is a young girl, then the score
        /// can potentially be negative, which will cause the method to throw an exception
        /// (which is also why PSI normally won't be applied for children)
        /// </summary>
        /// <param name="score">Score</param>
        /// <returns>Severity diagnosis</returns>
        /// <exception cref="InvalidOperationException">Throw if received invalid score</exception>
        public ScoreMetricsSeverityDiagnosis Psi(int score, List<string> evidences)
        {
            const string code = "PSI";

            // PSI return a more detail classification with 5 levels, and
            // 4 type of treatment site: 
            // 1. I - II: Outpatient (score <= 70)
            // 2. III: Inpatient short term (71 <= score <= 90)
            // 3. IV: Inpatient long term (91 <= score <= 130)
            // 4. V: Intensive Care Unit (score > 130)
            // Since we used our defined enums, we will convert with the following:
            // I - II: Mild + Outpatient
            // III: Moderate + Inpatient
            // IV: Severe + Inpatient
            // V: Severe + Intensive Care Unit
            return score switch
            {
                < 0 => throw new ArgumentOutOfRangeException(nameof(score), "Score cannot be negative"),
                >= 0 and <= 70 => new ScoreMetricsSeverityDiagnosis
                {
                    Code = code,
                    Score = score,
                    Severity = Severity.Mild,
                    TreatmentSite = TreatmentSite.Outpatient,
                    Evidences = evidences,
                },
                <= 90 => new ScoreMetricsSeverityDiagnosis
                {
                    Code = code,
                    Score = score,
                    Severity = Severity.Moderate,
                    TreatmentSite = TreatmentSite.Inpatient,
                    Evidences = evidences,
                },
                <= 130 => new ScoreMetricsSeverityDiagnosis
                {
                    Code = code,
                    Score = score,
                    Severity = Severity.Severe,
                    TreatmentSite = TreatmentSite.Inpatient,
                    Evidences = evidences,
                },
                _ => new ScoreMetricsSeverityDiagnosis
                {
                    Code = code,
                    Score = score,
                    Severity = Severity.Severe,
                    TreatmentSite = TreatmentSite.IntensiveCareUnit,
                    Evidences = evidences,
                },
            };
        }

        /// <summary>
        /// IDSA/ATS severity diagnosis. Note that, since IDSA/ATS is used to just
        /// check if patient need ICU or not, so if the not matched, then it will return
        /// null as result, and the final result should relied or other metrics.
        /// If matched, then the result would be (Severe, ICU)
        /// </summary>
        public MajorMinorMetricsSeverityDiagnosis? IdsaAts(ClinicalMetrics metrics, List<ClinicalObservation> observations)
        {
            if (!metrics.IsMajorMinorMetric)
            {
                throw new ArgumentException("IDSA/ATS required a Major/Minor metrics system");
            }

            var evidences = new List<string>();
            var majorMatched = metrics.Rules.Count(r =>
            {
                // Type casting to major/minor rule
                var rule = (MajorMinorRule)r;

                // If not major criteria, skip 
                if (!rule.IsMajor)
                {
                    return false;
                }

                // Evaluate major criteria
                var isSatisfied = rule.Criterion.IsCriterionSastisfied(observations);
                logger.LogDebug($"Evaluate major criteria for {metrics.Name}/{rule.Criterion.Name}: {isSatisfied}");

                evidences.Add(BuildEvidence($"{metrics.Name} - {rule.Criterion.Name}", rule.Criterion.Formula, observations));
                return isSatisfied;
            });

            var minorMatched = metrics.Rules.Count(r =>
            {
                // Type casting to major/minor rule
                var rule = (MajorMinorRule)r;

                // If not minor criteria, skip 
                if (rule.IsMajor)
                {
                    return false;
                }

                // Evaluate minor criteria
                var isSatisfied = rule.Criterion.IsCriterionSastisfied(observations);
                logger.LogDebug($"Evaluate minor criteria for {metrics.Name}/{rule.Criterion.Name}: {isSatisfied}");

                evidences.Add(BuildEvidence($"{metrics.Name} - {rule.Criterion.Name}", rule.Criterion.Formula, observations));
                return isSatisfied;
            });

            if (majorMatched >= 1 || minorMatched >= 3)
            {
                logger.LogDebug("IDSA/ATS matched: {detail}", new
                {
                    MajorMatched = majorMatched,
                    MinorMatched = minorMatched,
                });
                return new MajorMinorMetricsSeverityDiagnosis
                {
                    Code = metrics.Code,
                    MajorMatched = majorMatched,
                    MinorMatched = minorMatched,
                    Severity = Severity.Severe,
                    TreatmentSite = TreatmentSite.IntensiveCareUnit,
                    Evidences = evidences,
                };
            }

            logger.LogDebug("IDSA/ATS not matched");
            return null;
        }

        /// <summary>
        /// Assess infection based on diagnosis result, or based on risk factors.
        /// </summary>
        /// <param name="context">Clinical context</param>
        /// <param name="observations">Clinical observations</param>
        /// <param name="severity">Severity diagnosis</param>
        /// <param name="treatmentSite">Treatment site diagnosis</param>
        /// <returns>Infection assessment</returns>
        public InfectionAssessment AssessInfection(ClinicalContext context, IEnumerable<ClinicalObservation> observations, Severity severity, TreatmentSite treatmentSite)
        {
            /*
             * To assess infection, we will proceed with these steps:
             * 1. Check for suspected pathogens. This list is the easiest to check, but
             * since the only factors used is severity and treatment site, this may not
             * be a strong evidence for infection.
             * 2. Check for risk factors. Each satisfied risk factor counts 1 point,
             * and a pathogen that is also in the suspected list gets an extra point.
             * The final score of a pathogen simply is the number of satisfied risk
             * factors (plus the suspected boost).
             * 3. Assessment.
             * 3.1. Any pathogen with risk factors would be included in the
             * heavy suspected list.
             * 3.2. Any pathogen that is both in the suspected list and risk factors
             * would received a high boost, which make them appear first in the heavy
             * suspected list.
             * 3.3. If not, then the pathogen is worth considering
             */

            var heavySuspected = new List<(decimal score, Pathogen pathogen)>();
            var worthSuspected = new List<Pathogen>();
            var evidences = new List<string>();

            // Step 1: Check for suspected pathogens
            var suspected = context.SuspectedCauses
                .Where(sc => sc.Severity == severity && sc.TreatmentSite == treatmentSite)
                .Select(sc => sc.Pathogen)
                .ToList();

            // Step 2: Check for risk factors
            foreach (var pathogen in context.Pathogens)
            {
                if (pathogen.RiskFactors.Count == 0)
                {
                    logger.LogDebug("Pathogen {pathogen} has no risk factors, skipped", pathogen.Name);
                    continue;
                }

                // Calculate matched risk factors
                var matched = pathogen.RiskFactors.Count(r =>
                {
                    evidences.Add(BuildEvidence($"{pathogen.Name} - {r.Criterion.Name}", r.Criterion.Formula, observations));
                    return r.IsFactorSastified(observations);
                });

                if (matched == 0)
                {
                    logger.LogDebug("Pathogen {pathogen} has no matched risk factors, skipped", pathogen.Name);
                    continue;
                }

                // Calculate score
                var score = (decimal)matched / pathogen.RiskFactors.Count;
                logger.LogDebug("Risk factor calculate: {detail}", new
                {
                    pathogen.Name,
                    Matched = matched,
                    Total = pathogen.RiskFactors.Count,
                    Score = score,
                });

                // Check if this pathogen also exists in the suspected list
                if (suspected.Any(s => s.Id == pathogen.Id))
                {
                    // If yes, add a boost. The score counts satisfied risk factors,
                    // so +1 ranks a suspected pathogen above one with the same
                    // number of satisfied risk factors
                    logger.LogDebug("Pathogen {pathogen} is also in the suspected list, adding boost", pathogen.Name);
                    score++;
                }

                heavySuspected.Add((score, pathogen));
            }

            // Step 3: Assessment
            worthSuspected = [.. suspected.Where(s => !heavySuspected.Select(hs => hs.pathogen).Contains(s))];

            return new InfectionAssessment
            {
                HeavySuspected = [.. heavySuspected.OrderByDescending(hs => hs.score).Select(hs => hs.pathogen)],
                WorthSuspected = worthSuspected,
                Evidences = evidences,
            };
        }

        private Result ValidateObservations(ClinicalContext context, IEnumerable<ClinicalObservation> observations)
        {
            // Store all the errors for returning to client
            var errors = new Dictionary<Guid, string>();

            // First, check if all the required variables are present
            // Since this error is often the client dev team problem when implemeting,
            // not a user error, we won't return a detail message here
            var present = context.Variables
                .Where(v => v.IsRequired)
                .Select(x => x.Code)
                .All(v => observations.Select(o => o.Variable.Code).Contains(v));
            if (!present)
            {
                logger.LogDebug("Missing required variables, cannot proceed with diagnosis");
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Missing required variables"));
            }

            // Next, check internal validation rule of each observations
            foreach (var observation in observations)
            {
                if (observation.Variable.ValueType == ClinicalValueType.Numeric && !observation.Variable.IsValidValue(observation.NumericValue))
                {
                    logger.LogDebug("Invalid observation value for variable {code}: {value}", observation.Variable.Code, observation.NumericValue);

                    // Add the error to the list
                    var variable = (NumericClinicalVariable)observation.Variable;
                    errors.Add(variable.Id, $"{variable.Name} should be {variable.AcceptedRange}");
                    continue;
                }

                if (observation.Variable.ValueType == ClinicalValueType.Boolean && !observation.Variable.IsValidValue(observation.BooleanValue))
                {
                    logger.LogInformation("Invalid observation value for variable {code}: {value}", observation.Variable.Code, observation.BooleanValue);

                    // Add the error to the list (though this case is unlikely to happen because of C# type checking)
                    var variable = (BooleanClinicalVariable)observation.Variable;
                    errors.Add(variable.Id, $"{variable.Name} should be a valid boolean value");
                    continue;
                }

                if (observation.Variable.ValueType == ClinicalValueType.Categorical && !observation.Variable.IsValidValue(observation.CategoricalValue))
                {
                    logger.LogInformation("Invalid observation value for variable {code}: {value}", observation.Variable.Code, observation.CategoricalValue);

                    // Add the error to the list (this is also more of a client dev team problem that user problem),
                    // so it's rare to happen
                    var variable = (CategoricalClinicalVariable)observation.Variable;
                    errors.Add(variable.Id, $"{variable.Name} should be in this value range {string.Join(", ", variable.AcceptedValues)}");
                }
            }

            // Validation on variables that has prerequisite formula
            foreach (var variable in context.Variables.Where(x => x.Prerequisite is not null))
            {
                if (!(bool)variable.Prerequisite!.ToExpression(observations).Evaluate())
                {
                    logger.LogDebug("Prerequisite formula for variable {code} is not satisfied", variable.Code);
                    errors.Add(variable.Id, $"Prerequisite ({variable.Prerequisite}) of variable {variable.Code} is not satisfied");
                }
            }

            if (errors.Count > 0)
            {
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Invalid observation value", errors));
            }

            return Result.Success(ApplicationStatus.Success);
        }

        public Result<Diagnosis> Diagnose(ClinicalContext context, ClinicalPicture clinicalPicture)
        {
            // Internal validation
            var validationResult = ValidateObservations(context, clinicalPicture.Observations);
            if (validationResult.IsFailure())
            {
                logger.LogDebug("Validation failed: {detail}", validationResult.Error);
                return Result<Diagnosis>.Failure(validationResult.Error!);
            }

            // Store the severity diagnosis for final judgement
            var severityDiagnosis = new HashSet<(Severity severity, TreatmentSite treatmentSite)>();

            // Start evaluated metrics
            var observations = clinicalPicture.Observations;
            var totalEvidences = new List<string>();
            foreach (var metric in context.Metrics)
            {
                if (metric.Code.Equals("CURB-65"))
                {
                    // Check if BUN is missing
                    var isBunMissing = !observations.Any(x => x.Variable.Code.Equals("UREA"));

                    // Perform diagnosis
                    var (score, evidences) = CalculateMetricsScore(metric, observations);
                    var diagnosis = Curb65((int)score, evidences, isBunMissing);
                    severityDiagnosis.Add((diagnosis.Severity, diagnosis.TreatmentSite));
                    totalEvidences.AddRange(evidences);

                    logger.LogInformation("CURB-65 diagnosis: {diagnosis}", diagnosis);
                }
                else if (metric.Code.Equals("PSI"))
                {
                    var (score, evidences) = CalculateMetricsScore(metric, observations);
                    var diagnosis = Psi((int)score, evidences);
                    severityDiagnosis.Add((diagnosis.Severity, diagnosis.TreatmentSite));
                    totalEvidences.AddRange(evidences);
                    logger.LogInformation("PSI diagnosis: {diagnosis}", diagnosis);
                }
                else if (metric.Code.Equals("IDSA/ATS"))
                {
                    var diagnosis = IdsaAts(metric, observations);
                    if (diagnosis is not null)
                    {
                        severityDiagnosis.Add((diagnosis.Severity, diagnosis.TreatmentSite));
                        totalEvidences.AddRange(diagnosis.Evidences);
                    }
                    logger.LogInformation("IDSA/ATS diagnosis: {diagnosis}", diagnosis);
                }
                else
                {
                    logger.LogWarning($"Metric {metric.Code} is not supported");
                }
            }

            // If there are several severity diagnosis, then we will choose the highest one
            Severity finalSeverity;
            TreatmentSite finalTreatmentSite;
            if (severityDiagnosis.Count > 1)
            {
                (finalSeverity, finalTreatmentSite) = severityDiagnosis
                    .OrderByDescending(x => x.severity)
                    .ThenByDescending(x => x.treatmentSite)
                    .First();
            }
            else if (severityDiagnosis.Count == 1)
            {
                (finalSeverity, finalTreatmentSite) = severityDiagnosis.First();
            }
            else
            {
                logger.LogWarning("No severity diagnosis found");
                return Result<Diagnosis>.Failure(new Error(ApplicationStatus.ServerError, "Failed to perform severity diagnosis"));
            }

            // Assess infection
            var infectionAssessment = AssessInfection(context, observations, finalSeverity, finalTreatmentSite);
            totalEvidences.AddRange(infectionAssessment.Evidences);

            // Get the list of missing variables
            var missing = context.Variables
                .Where(v => !observations.Select(o => o.Variable.Code).Contains(v.Code))
                .ToList();

            return Result<Diagnosis>.Success(ApplicationStatus.Success, new Diagnosis
            {
                Severity = finalSeverity,
                TreatmentSite = finalTreatmentSite,
                Evidences = totalEvidences,
                HeavySuspected = infectionAssessment.HeavySuspected,
                WorthSuspected = infectionAssessment.WorthSuspected,
                MissingVariables = missing,
                Allergies = clinicalPicture.Allergies,
                IsPatientPregnantOrInLactationPhase = observations.Any(x => x.Variable.Code.Equals("PREGNANT-OR-LACTATING") && x.BooleanValue == true),
            });
        }
    }
}
