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
        /// Calculate eGFR using MDRD equation
        /// </summary>
        /// <param name="age">Patient's age</param>
        /// <param name="scr">Serum creatine used (mmol/L)</param>
        /// <param name="isFemale">Is patient female</param>
        /// <param name="isBlackAmerican">Is patient black american</param>
        /// <returns>eGFR (in mL/minute/1.73 m2)</returns>
        internal double CalculateEgfr(int age, double scr, bool isFemale, bool isBlackAmerican)
        {
            if (age <= 0) throw new ArgumentException("Age must be positive integer");
            if (scr <= 0) throw new ArgumentException("Serum creatine must be positive number");

            var egfr = 175 * Math.Pow(scr / 88.4, -1.154) * Math.Pow(age, -0.203);
            if (isFemale)
            {
                egfr *= 0.742;
            }

            if (isBlackAmerican)
            {
                egfr *= 1.212;
            }

            return egfr;
        }

        /// <summary>
        /// Calculate metrics score
        /// </summary>
        /// <param name="metric">Metrics</param>
        /// <param name="observations">Clinical observations</param>
        /// <returns>Score result</returns>
        public (decimal, List<Evidence>) CalculateMetricsScore(ClinicalMetric metric, IEnumerable<ClinicalObservation> observations)
        {
            observations = observations.ToList().AsReadOnly();

            if (metric.IsMajorMinorMetric) throw new ArgumentException("Cannot calculate score for a Major/Minor metrics system");

            var evidences = new List<Evidence>();
            var totalScore = metric.Rules.Sum(r =>
            {
                // Type casting to scoring rule
                var rule = (ScoringRule)r;

                // Calculate score
                var score = rule.GetScore(observations);

                // Add evidence
                var source = $"{metric.Name} - {rule.Criterion.Name}";
                var evidence = new Evidence(source, rule.Criterion.Formula, [.. rule.Variables], [.. observations], score);
                evidences.Add(evidence);

                logger.LogDebug("Calculate scoring rule: {Source} = {Score}", source, score);

                // Return score
                return score;
            });

            return (totalScore, evidences);
        }

        /// <summary>
        /// CURB-65 severity diagnosis.
        /// </summary>
        /// <param name="score">Score</param>
        /// <param name="evidences">List of evidences found when evaluating this metric</param>
        /// <returns>Severity diagnosis</returns>
        /// <exception cref="InvalidOperationException">Throw if received invalid score</exception>
        public ScoreMetricsSeverityDiagnosis Curb65(int score, List<Evidence> evidences)
        {
            /*
             * CURB-65 (according to our internal definition):
             * For CURB-65:
             * 1. 0 - 1: Mild + Outpatient
             * 2. 2: Moderate + Inpatient
             * 3. 3: Severe + Inpatient
             * 4. 4 - 5: Severe + Intensive Care Unit
             */

            const string code = "CURB-65";
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
        /// CURB-65 severity diagnosis.
        /// </summary>
        /// <param name="score">Score</param>
        /// <param name="evidences">List of evidences found when evaluating this metric</param>
        /// <returns>Severity diagnosis</returns>
        /// <exception cref="InvalidOperationException">Throw if received invalid score</exception>
        public ScoreMetricsSeverityDiagnosis Crb65(int score, List<Evidence> evidences)
        {
            /*
             * CURB-65 (according to our internal definition):
             * For CRB-65:
             * 1. 0: Mild + Outpatient
             * 2. 1 - 2: Moderate + Inpatient
             * 3. 3: Severe + Inpatient
             * 4. 4: Severe + Intensive Care Unit
             */

            const string code = "CRB-65";
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
            };
        }

        /// <summary>
        /// PSI severity diagnosis. Note that, if patient is a young girl, then the score
        /// can potentially be negative, which will cause the method to throw an exception
        /// (which is also why PSI normally won't be applied for children)
        /// </summary>
        /// <param name="score">Score</param>
        /// <param name="evidences">List of evidences found when evaluating this metric</param>
        /// <returns>Severity diagnosis</returns>
        /// <exception cref="InvalidOperationException">Throw if received invalid score</exception>
        public ScoreMetricsSeverityDiagnosis Psi(int score, List<Evidence> evidences)
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
                <= 70 => new ScoreMetricsSeverityDiagnosis
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
        /// null as result, and the final result should rely on or other metrics.
        /// If matched, then the result would be (Severe, ICU)
        /// </summary>
        public MajorMinorMetricsSeverityDiagnosis? IdsaAts(ClinicalMetric metric, IEnumerable<ClinicalObservation> observations)
        {
            observations = observations.ToList().AsReadOnly();

            if (!metric.IsMajorMinorMetric) throw new ArgumentException("IDSA/ATS required a Major/Minor metrics system");

            var evidences = new List<Evidence>();
            var majorMatched = metric.Rules.Count(r =>
            {
                // Type casting to major/minor rule
                var rule = (MajorMinorRule)r;

                // If not major criteria, skip 
                if (!rule.IsMajor) return false;

                // Evaluate major criteria
                var isSatisfied = rule.Criterion.IsCriterionSatisfied(observations);

                // Add evidence
                var source = $"{metric.Name} - {rule.Criterion.Name}";
                var evidence = new Evidence(source, rule.Criterion.Formula, [.. rule.Variables], [.. observations], true);
                evidences.Add(evidence);
                logger.LogDebug("Evaluate major rule: {Source} = {Evaluation}", source, isSatisfied);

                return isSatisfied;
            });

            var minorMatched = metric.Rules.Count(r =>
            {
                // Type casting to major/minor rule
                var rule = (MajorMinorRule)r;

                // If not minor criteria, skip 
                if (rule.IsMajor) return false;

                // Evaluate minor criteria
                var isSatisfied = rule.Criterion.IsCriterionSatisfied(observations);

                // Add evidence
                var source = $"{metric.Name} - {rule.Criterion.Name}";
                var evidence = new Evidence(source, rule.Criterion.Formula, [.. rule.Variables], [.. observations], false);
                evidences.Add(evidence);
                logger.LogDebug("Evaluate minor rule: {Source} = {Evaluation}", source, isSatisfied);

                return isSatisfied;
            });

            if (majorMatched >= 1 || minorMatched >= 3)
            {
                logger.LogDebug("IDSA/ATS satisfied: Major matched = {MajorMatched}; Minor Mathed = {MinorMatched}", majorMatched, minorMatched);
                return new MajorMinorMetricsSeverityDiagnosis
                {
                    Code = metric.Code,
                    MajorMatched = majorMatched,
                    MinorMatched = minorMatched,
                    Severity = Severity.Severe,
                    TreatmentSite = TreatmentSite.IntensiveCareUnit,
                    Evidences = evidences,
                };
            }

            logger.LogDebug("IDSA/ATS not satisfied");
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
             * Infection assessment: this method assess patient's risk of infection of common pathogen.
             * The result is categorized into 2 lists: 
             * 1. Heavy suspected: these pathogen has clear evidences using risk factors,
             * doctor and recommendation engine should focus on this list. This list can
             * still be empty, if patient's syntomps does not include any risk factors
             * 1.1. A pathogen can only be added if its score exceed 50%
             * 1.2. The result is not sorted, since a patient can be infected to multiple
             * pathogens at once, and the treatment should cover all of them, so ranking
             * would lead to some misleading behaviors
             * 1.3. Rule for calculating score: a normal factor default to 1, so its score
             * is basically 1/total. A critical factor default to 100% (in other word, score = total)
             * 2. Worth suspected: this list should be used as a reference (for example, if the treatment
             * failed based on heavy suspected pathogen, then doctor can go back to this record and try
             * inspecting on these worth suspected pathogens). This list shouldn't affect the recommendation
             * engine (or rather, this list should just be used to show, nothing more).
             * 2.1. This list should be not empty, as long as the data is not deleted, or clinical context is
             * populated correctly
             * 2.2. If a pathogen is in this worth suspected list and has risk factors, they will be remove
             * from the worth suspected list
             */

            observations = observations.ToList().AsReadOnly();

            var heavySuspected = new List<Pathogen>();
            var evidences = new List<Evidence>();

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
                    logger.LogDebug("Pathogen {PathogenName} has no risk factors, skipped", pathogen.Name);
                    continue;
                }

                // Calculate matched risk factors
                var matchedAsNonCritical = pathogen.RiskFactors
                    .Where(r => !r.IsCriticalFactor)
                    .Count(r =>
                    {
                        // Generate evidence
                        var source = $"{pathogen.Name} - {r.Criterion.Name}";
                        var evidence = new Evidence(source, r.Criterion.Formula, [.. r.Criterion.Variables], [.. observations]);
                        evidences.Add(evidence);

                        // Evaluate factor
                        var isSatisfied = r.IsFactorSatisfied(observations);
                        logger.LogDebug("Evaluate non-critical risk factor: {Source} = {Evaluation}", source, isSatisfied);
                        return isSatisfied;
                    });

                var matchedAsCritical = pathogen.RiskFactors
                    .Where(r => r.IsCriticalFactor)
                    .Count(r =>
                {
                    // Generate evidence
                    var source = $"{pathogen.Name} - {r.Criterion.Name}";
                    var evidence = new Evidence(source, r.Criterion.Formula, [.. r.Criterion.Variables], [.. observations]);
                    evidences.Add(evidence);

                    // Evaluate factor
                    var isSatisfied = r.IsFactorSatisfied(observations);
                    logger.LogDebug("Evaluate critical risk factor: {Source} = {Evaluation}", source, isSatisfied);
                    return isSatisfied;
                });

                // If no risk factor satisfied, skip to the next pathogen
                if (matchedAsNonCritical + matchedAsCritical == 0)
                {
                    logger.LogDebug("Pathogen {PathogenName} has no matched risk factors, skipped", pathogen.Name);
                    continue;
                }

                // Since the threshold is exactly 50%, we can avoid division by checking if 
                // any critical factor matched (they equal 100%, so obviously passed), or if
                // double the non critical factor matched greater or equal than total risk factors
                if (matchedAsCritical > 0 || 2 * matchedAsNonCritical >= pathogen.RiskFactors.Count)
                {
                    logger.LogDebug("Risk factor calculate: {Detail}", new
                    {
                        pathogen.Name,
                        MatchedAsNonCritical = matchedAsNonCritical,
                        MatchedAsCritical = matchedAsCritical,
                        Total = pathogen.RiskFactors.Count,
                    });
                    heavySuspected.Add(pathogen);

                    // Check if this pathogen also exists in the suspected list
                    if (suspected.Any(s => s.Id == pathogen.Id))
                    {
                        // If this pathogen also in suspected list, then remove it from the suspected list
                        // to avoid duplicate
                        logger.LogDebug("Pathogen {PathogenName} in worth suspected list and has risk factor", pathogen.Name);
                        suspected.RemoveAll(x => x.Id == pathogen.Id);
                    }
                }
            }

            // Step 3: Assessment
            return new InfectionAssessment
            {
                HeavySuspected = heavySuspected,
                WorthSuspected = suspected,
                Evidences = evidences,
            };
        }

        /// <summary>
        /// Check if antibiotic groups has common ancestor. For example, monobactam and cephalosporin
        /// has beta-lactam as common ancestor.</br>
        /// Business rule: each treatment medicines should belong to the same antibiotic group linage.
        /// For example, for a treatment medicines:</br>
        /// 1. Carbapenem + Cephalosporin -> invalid (both belong to beta-lactam linage)</br>
        /// 2. Cephalosporin + Macrolid -> valid (Macrolid does not belong to beta-lactam linage)</br>
        /// </summary>
        /// <param name="context">Clinical context</param>
        /// <param name="group1">Antibiotic group 1</param>
        /// <param name="group2">Antibiotic group 2</param>
        /// <returns>Result, true if  groups has same command ancestor</returns>
        public Result<bool> IsAntibioticGroupHasCommonAncestor(ClinicalContext context, AntibioticGroup group1, AntibioticGroup group2)
        {
            // Check if group 1 and group 2 is in context
            if (!context.AntibioticGroups.Any(g => g.Id.Equals(group1.Id)))
            {
                return Result<bool>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Antibiotic group 1 not found"));
            }

            if (!context.AntibioticGroups.Any(g => g.Id.Equals(group2.Id)))
            {
                return Result<bool>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Antibiotic group 2 not found"));
            }

            // Build the linage list of group 1 and group 2
            var linage1 = new List<AntibioticGroup>();
            AntibioticGroup? temp = group1;
            do
            {
                linage1.Add(temp);
                temp = temp.Parent;
            } while (temp is not null);

            var linage2 = new List<AntibioticGroup>();
            temp = group2;
            do
            {
                linage2.Add(temp);
                temp = temp.Parent;
            } while (temp is not null);

            // Check if any of the linage 1 is in linage 2 or vice versa
            var isSame = linage1.Any(linage2.Contains);
            return Result<bool>.Success(ApplicationStatus.Success, isSame);
        }

        /// <summary>
        /// Internal validation of each clinical observation
        /// </summary>
        /// <param name="context">Clinical context</param>
        /// <param name="observations">Clinical observations</param>
        /// <returns>Boolean result, true if valid</returns>
        internal Result ValidateObservations(ClinicalContext context, IEnumerable<ClinicalObservation> observations)
        {
            observations = observations.ToList().AsReadOnly();

            // Store all the errors for returning to client
            var errors = new Dictionary<Guid, string>();

            // First, check if all the required variables are present
            // Since this error is often the client dev team problem when implementing,
            // not a user error, we won't return a detail message here
            var present = context.Variables
                .Where(v => v.IsRequired)
                .Select(x => x.Code)
                .All(v => observations.Select(o => o.Variable.Code).Contains(v));
            if (!present)
            {
                logger.LogDebug("Missing required variables, cannot proceed with diagnosis");
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Missing required variables"));
            }

            // Next, check internal validation rule of each observation
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

            var variables = observations.Select(x => x.Variable).ToList();

            // Validation on supplied variables that has prerequisite formula
            // foreach (var variable in context.Variables.Where(x => x.Prerequisite is not null))
            foreach (var variable in variables.Where(v => v.Prerequisite is not null))
            {
                // Check if all the variables used for the prerequisite formula are present
                if (!variable.Prerequisite!.Variables.All(v => variables.Select(x => x.Code).Contains(v.Code)))
                {
                    var msg = $"Varible {variable.Code}'s prerequisite formula required all this variables: {string.Join(", ", variable.Prerequisite!.Variables.Select(v => v.Code))}";
                    logger.LogDebug("Prerequisite formula for variable {Code} is not satisfied: {Message}", variable.Code, msg);
                    errors.Add(variable.Id, msg);
                }

                // Evaluate prerequisite formula
                if (!(bool)variable.Prerequisite!.ToExpression(observations).Evaluate())
                {
                    logger.LogDebug("Prerequisite formula for variable {code} is not satisfied", variable.Code);
                    errors.Add(variable.Id, $"Prerequisite ({variable.Prerequisite}) of variable {variable.Code} is not satisfied");
                }
            }

            return errors.Count > 0
                ? Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Invalid observation value", errors))
                : Result.Success(ApplicationStatus.Success);
        }

        /// <summary>
        /// Severity and infection diagnosis
        /// </summary>
        /// <param name="context">Clinical context</param>
        /// <param name="observations">Clinical observations</param>
        /// <returns>Diagnosis result</returns>
        public Result<Diagnosis> Diagnose(ClinicalContext context, IEnumerable<ClinicalObservation> observations)
        {
            observations = observations.ToList().AsReadOnly();

            // Internal validation
            var validationResult = ValidateObservations(context, observations);
            if (validationResult.IsFailure())
            {
                logger.LogDebug("Validation failed: {detail}", validationResult.Error);
                return Result<Diagnosis>.Failure(validationResult.Error!);
            }

            // Store the severity diagnosis for final judgment
            var severityDiagnosis = new HashSet<(Severity severity, TreatmentSite treatmentSite)>();

            // Start evaluated metrics
            var totalEvidences = new List<Evidence>();
            var metricsSeverityDiagnosis = new List<MetricsSeverityDiagnosis>();
            foreach (var metric in context.Metrics)
            {
                if (metric.Code.Equals("CURB-65"))
                {
                    // Perform diagnosis
                    var (score, evidences) = CalculateMetricsScore(metric, observations);
                    var diagnosis = Curb65((int)score, evidences);
                    metricsSeverityDiagnosis.Add(diagnosis);
                    severityDiagnosis.Add((diagnosis.Severity, diagnosis.TreatmentSite));
                    totalEvidences.AddRange(evidences);

                    logger.LogInformation("CURB-65 diagnosis: {diagnosis}", diagnosis);
                }
                else if (metric.Code.Equals("CRB-65"))
                {
                    // Perform diagnosis
                    var (score, evidences) = CalculateMetricsScore(metric, observations);
                    var diagnosis = Crb65((int)score, evidences);
                    metricsSeverityDiagnosis.Add(diagnosis);
                    severityDiagnosis.Add((diagnosis.Severity, diagnosis.TreatmentSite));
                    totalEvidences.AddRange(evidences);

                    logger.LogInformation("CRB-65 diagnosis: {diagnosis}", diagnosis);

                }
                else if (metric.Code.Equals("PSI"))
                {
                    var (score, evidences) = CalculateMetricsScore(metric, observations);
                    var diagnosis = Psi((int)score, evidences);
                    metricsSeverityDiagnosis.Add(diagnosis);
                    severityDiagnosis.Add((diagnosis.Severity, diagnosis.TreatmentSite));
                    totalEvidences.AddRange(evidences);
                    logger.LogInformation("PSI diagnosis: {diagnosis}", diagnosis);
                }
                else if (metric.Code.Equals("IDSA/ATS"))
                {
                    var diagnosis = IdsaAts(metric, observations);
                    if (diagnosis is not null)
                    {
                        metricsSeverityDiagnosis.Add(diagnosis);
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

            // If there are some severity diagnosis, then we will choose the highest one
            (Severity severity, TreatmentSite treatmentSite) final;
            switch (severityDiagnosis.Count)
            {
                case > 1:
                    final = severityDiagnosis
                        .OrderByDescending(x => x.severity)
                        .ThenByDescending(x => x.treatmentSite)
                        .First();
                    break;
                // Practically, this case wouldn't happen, since both CURB-65, CRB-65 and PSI
                // would yield result since some input are required 
                case 1:
                    final = severityDiagnosis.First();
                    break;
                // Same, as above, this is non-practical since both CURB-65, CRB-65 and PSI
                // would yield result
                default:
                    logger.LogWarning("No severity diagnosis found");
                    return Result<Diagnosis>.Failure(new ApplicationError(ApplicationStatus.ServerError, "Failed to perform severity diagnosis"));
            }

            // Assess infection
            var infectionAssessment = AssessInfection(context, observations, final.severity, final.treatmentSite);
            totalEvidences.AddRange(infectionAssessment.Evidences);

            // Get the list of missing variables
            var missing = context.Variables
                .Where(v => !observations.Select(o => o.Variable.Code).Contains(v.Code))
                .ToList();

            // Calculate eGFR
            var scr = observations.FirstOrDefault(x => x.Variable.Code.Equals("CREATINE"));
            double? egfr = null;
            if (scr is not null)
            {
                logger.LogDebug("Serum creatine provided, start calculate eGFR");

                // AGE, GENDER and BLACK-AMERICAN should be required variables,
                // so if they are null, it should be an exception, not an expected
                // failure behavior
                var age = (int)observations.First(x => x.Variable.Code.Equals("AGE")).NumericValue!;
                var isFemale = observations.First(x => x.Variable.Code.Equals("GENDER")).CategoricalValue!.Equals("FEMALE");
                var isBlackAmerican = (bool)observations.First(x => x.Variable.Code.Equals("BLACK-AMERICAN")).BooleanValue!;
                egfr = CalculateEgfr(age, (double)scr.NumericValue!, isFemale, isBlackAmerican);
            }

            return Result<Diagnosis>.Success(ApplicationStatus.Success, new Diagnosis
            {
                Severity = final.severity,
                TreatmentSite = final.treatmentSite,
                MetricsSeverityDiagnosis = metricsSeverityDiagnosis,
                Evidences = totalEvidences,
                HeavySuspected = infectionAssessment.HeavySuspected,
                WorthSuspected = infectionAssessment.WorthSuspected,
                MissingVariables = missing,
                IsPatientPregnantOrInLactationPhase = observations.Any(x => x.Variable.Code.Equals("PREGNANT-OR-LACTATING") && x.BooleanValue == true),
                Egfr = egfr
            });
        }

        /// <summary>
        /// Get result from microbial sample test
        /// </summary>
        /// <param name="context">Clinical context</param>
        /// <param name="samples">Microbial samples</param>
        /// <returns>Antibiograms result</returns>
        public Result<IEnumerable<Antibiogram>> GetMicrobiologicalTestResult(ClinicalContext context, IEnumerable<MicrobialSample> samples)
        {
            var antibiograms = new List<Antibiogram>();

            foreach (var sample in samples)
            {
                var antibiogram = context.Antibiograms
                    .FirstOrDefault(x => x.PathogenId == sample.PathogenId && x.MicCategory == sample.MicCategory);
                if (antibiogram is null)
                {
                    logger.LogDebug("Couldn't find any antibiogram that match this sample: {Sample}", sample);
                    continue;
                }

                antibiograms.Add(antibiogram);
            }

            return Result<IEnumerable<Antibiogram>>.Success(ApplicationStatus.Success, antibiograms);
        }

        /// <summary>
        /// Treatment recommendation. This method recommend treatment protocols based on patient's clinical
        /// picture and diagnosis result
        /// </summary>
        /// <param name="context">Clinical context</param>
        /// <param name="clinicalPicture">Clinical picture</param>
        /// <param name="diagnosis">Diagnosis result</param>
        /// <returns>List of treatment protocols used</returns>
        public Result<IEnumerable<Treatment>> RecommendTreatment(ClinicalContext context, ClinicalPicture clinicalPicture, Diagnosis diagnosis)
        {
            // Get the list of candidate protocols using diagnosis result
            var candidates = context.TreatmentProtocols
                .Where(x => x.Severity == diagnosis.Severity && x.TreatmentSite == diagnosis.TreatmentSite);

            // As long as data is persisted correctly and loaded correctly, this shouldn't happen
            if (!candidates.Any())
            {
                logger.LogDebug("Failed to find any candidate treatment protocols");
                return Result<IEnumerable<Treatment>>.Failure(new ApplicationError(ApplicationStatus.Failure, "Failed to find any candidate treatment protocols"));
            }

            // Filter by criteria: only pick protocols where their criteria all matched
            logger.LogDebug("Start filtering candidates by criteria using clinical observation");
            var filtererByCriteria = candidates.Where(x => x.Criteria.All(c => c.IsCriterionSatisfied(clinicalPicture.Observations)));
            logger.LogDebug("Done filtering candidates by criteria: {Detail}", new
            {
                CandidateCount = candidates.Count(),
                FilterByCriteria = filtererByCriteria.Count(),
            });

            // Filter by pathogens: prioritize protocols that has wider cover for pathogens.
            // For example, infecction assessment have pseudomonas and MRSA, and there are
            // have 3 protocols, one cover pseudomonas, one cover MRSA and one cover
            // BOTH pseudomonas and MRSA, then the third one must be picked, while the other 2 don't

            logger.LogDebug("Start filtering candidates by pathogens");

            // First, since the treatment protocol Pathogens list is equivalent to AND (which means that
            // this treatment protocol can only be applied if ALL the pathogens is suspected), so any protocols
            // that have a pathogen that didn't exists in heavy suspected list
            var filteredByPathogen = filtererByCriteria
                .Where(x => x.Pathogens
                        .Select(p => p.Id)
                        .All(p => diagnosis.HeavySuspected.Select(hs => hs.Id).Contains(p)))
                .ToList();

            // Filter by dominance. This only make sense if the heavt suspected has more than
            // one pathogen
            if (diagnosis.HeavySuspected.Count() >= 2)
            {
                // Materialize the candidate list before reassigning, otherwise the lambda
                // below would capture the reassigned variable and recurse into itself
                var dominanceCandidates = filteredByPathogen;

                // Any protocol that exists another protocol that has a larger and 
                // include all the pathogens in its list should be dropped
                filteredByPathogen = [.. dominanceCandidates
                    .Where(protocol => !dominanceCandidates.Any(other =>
                            other.Id != protocol.Id &&
                            other.Pathogens.Count > protocol.Pathogens.Count &&
                            protocol.Pathogens.Select(x => x.Id).All(p => other.Pathogens.Select(x => x.Id).Contains(p))))];
            }

            logger.LogDebug("Done filtering candidates by pathogens: {Detail}", new
            {
                FiltererByCriteriaCount = filtererByCriteria.Count(),
                FilterByPathogenCount = filteredByPathogen.Count
            });

            var protocols = filteredByPathogen.ToList();

            // The final result shouldn't be empty if data is correct
            if (protocols.Count == 0)
            {
                logger.LogDebug("No treatment protocols found after applying filter");
            }

            // Filter any treatment that contains antibiotic that patient is allergic to
            foreach (var allergy in clinicalPicture.Allergy.AllergicAntibiotics)
            {
                foreach (var protocol in protocols)
                {
                    // Materialize first: removing from a ToList() copy would leave the
                    // treatment's own collection untouched
                    var allergicCompositions = protocol.Medicines
                        .Where(composition => composition.Antibiotics.Any(a => a.Id == allergy.Id))
                        .ToList();

                    foreach (var composition in allergicCompositions)
                    {
                        protocol.Medicines.Remove(composition);
                    }
                }
            }

            // If the allergy filter make the protocol's medicines list become empty as well,
            // then remove that protocol, since it serves no purpose anymore
            protocols.RemoveAll(p => !p.Medicines.Any());

            return Result<IEnumerable<Treatment>>.Success(ApplicationStatus.Success, protocols);
        }
    }
}
