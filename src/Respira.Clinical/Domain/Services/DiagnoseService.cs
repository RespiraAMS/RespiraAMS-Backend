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
        /// Calculate metrics score
        /// </summary>
        /// <param name="metrics">Metrics</param>
        /// <param name="observations">Clinical observations</param>
        /// <returns>Score result</returns>
        public (decimal, List<string>) CalculateMetricsScore(ScoreMetrics metrics, IEnumerable<ClinicalObservation> observations)
        {
            var evidences = new List<string>();
            var totalScore = metrics.ScoringRules.Sum(sr =>
            {
                var score = sr.GetScore(observations);
                logger.LogDebug($"Calculated score for {metrics.Name}/{sr.Criterion.Name}: {score}");
                evidences.Add(BuildEvidence($"{metrics.Name} - {sr.Criterion.Name}", sr.Criterion.Formula, observations));
                return score;
            });

            return (totalScore, evidences);
        }

        /// <summary>
        /// Render an evidence entry for a criterion. A formula cannot be evaluated when the
        /// observations do not contain every variable it uses, so the evidence reports the
        /// missing variables instead of throwing (the score itself is already handled by
        /// <see cref="Criterion.IsCriterionSatisfied(IEnumerable{ClinicalObservation})"/>).
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
        /// CURB-65 severity diagnosis
        /// </summary>
        /// <param name="score">Score</param>
        /// <returns>Severity diagnosis</returns>
        /// <exception cref="InvalidOperationException">Throw if received invalid score</exception>
        public MetricsSeverityDiagnosis Curb65(int score, List<string> evidences, bool isBunMissing = false)
        {
            string code = isBunMissing ? "CRB-65" : "CURB-65";
            // If BUN is missing (to be more exact, urea), then this would still be valid
            // (CRB-65, which has different value matching)
            if (isBunMissing)
            {
                return score switch
                {
                    < 0 => throw new ArgumentOutOfRangeException(nameof(score), "Score cannot be negative"),
                    0 => new MetricsSeverityDiagnosis(code, score, Severity.Mild, TreatmentSite.Outpatient, evidences),
                    1 or 2 => new MetricsSeverityDiagnosis(code, score, Severity.Moderate, TreatmentSite.Inpatient, evidences),
                    3 or 4 => new MetricsSeverityDiagnosis(code, score, Severity.Severe, TreatmentSite.Inpatient, evidences),
                    _ => throw new InvalidOperationException($"Unexpected CRB-65 score: {score}"),
                };
            }

            // If BUN is present, then this would be CURB-65
            return score switch
            {
                < 0 => throw new ArgumentOutOfRangeException(nameof(score), "Score cannot be negative"),
                0 or 1 => new MetricsSeverityDiagnosis(code, score, Severity.Mild, TreatmentSite.Outpatient, evidences),
                2 => new MetricsSeverityDiagnosis(code, score, Severity.Moderate, TreatmentSite.Inpatient, evidences),
                >= 3 and <= 5 => new MetricsSeverityDiagnosis(code, score, Severity.Severe, TreatmentSite.Inpatient, evidences),
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
        public MetricsSeverityDiagnosis Psi(int score, List<string> evidences)
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
                >= 0 and <= 70 => new MetricsSeverityDiagnosis(code, score, Severity.Mild, TreatmentSite.Outpatient, evidences),
                <= 90 => new MetricsSeverityDiagnosis(code, score, Severity.Moderate, TreatmentSite.Inpatient, evidences),
                <= 130 => new MetricsSeverityDiagnosis(code, score, Severity.Severe, TreatmentSite.Inpatient, evidences),
                _ => new MetricsSeverityDiagnosis(code, score, Severity.Severe, TreatmentSite.IntensiveCareUnit, evidences),
            };
        }

        /// <summary>
        /// AST severity diagnosis. Note that, the actual IDSA/ATS use a major/minor criteria
        /// system, which we have converted into a score system. Currently, this is still correct,
        /// but it would be completely wrong if the scale was changed (e.g. required both major
        /// and minor criteria to be met)
        /// </summary>
        /// <param name="score">AST score</param>
        /// <returns>True if need ICU, false otherwise</returns>
        public bool Ats(int score)
        {
            // AST metrics actually used to check if you need ICU or not
            // Since ICU case is actually severe already, we will return
            // Severe if true, while false would be the severity and 
            // treatment site passed in parameter
            // Note that a case can still be severe without ICU
            return score >= 3;
        }

        public Result<SeverityDiagnosis> DiagnoseSeverity(ClinicalContext context, IEnumerable<ClinicalObservation> observations)
        {
            // We will prioritize the highest severity and treatment site,
            // but it must be a valid combination. For example, if severity
            // is severe, but treatment site is outpatient, which is obviously
            // invalid
            var severities = new Dictionary<string, Severity>();
            var treatmentSites = new Dictionary<string, TreatmentSite>();
            IEnumerable<MetricsSeverityDiagnosis> metricsDiagnoses = [];

            foreach (var metric in context.Metrics)
            {
                if (metric.Code.Equals("CURB-65"))
                {
                    // Check if BUN is missing
                    var isBunMissing = !observations.Any(x => x.Variable.Code.Equals("UREA"));

                    var (score, evidences) = CalculateMetricsScore(metric, observations);
                    var diagnosis = Curb65((int)score, evidences, isBunMissing);
                    metricsDiagnoses = metricsDiagnoses.Append(diagnosis);
                    severities.Add(metric.Code, diagnosis.Severity);
                    treatmentSites.Add(metric.Code, diagnosis.TreatmentSite);
                }
                else if (metric.Code.Equals("PSI"))
                {
                    var (score, evidences) = CalculateMetricsScore(metric, observations);
                    var diagnosis = Psi((int)score, evidences);
                    metricsDiagnoses = metricsDiagnoses.Append(diagnosis);
                    severities.Add(metric.Code, diagnosis.Severity);
                    treatmentSites.Add(metric.Code, diagnosis.TreatmentSite);
                }
                else if (metric.Code.Equals("IDSA/ATS"))
                {
                    var (score, evidences) = CalculateMetricsScore(metric, observations);
                    var needIcu = Ats((int)score);
                    if (needIcu)
                    {
                        // If you need ICU, then the severity is obviously severe
                        metricsDiagnoses = metricsDiagnoses.Append(new MetricsSeverityDiagnosis(
                            metric.Code,
                            (int)score,
                            Severity.Severe,
                            TreatmentSite.IntensiveCareUnit,
                            evidences));
                        severities.Add(metric.Code, Severity.Severe);
                        treatmentSites.Add(metric.Code, TreatmentSite.IntensiveCareUnit);
                    }
                }
                else
                {
                    logger.LogWarning($"Metric {metric.Code} is not supported");
                    throw new NotSupportedException($"Metric {metric.Code} is not supported");
                }
            }

            logger.LogDebug("Diagnosis result: {detail}", new
            {
                Severities = severities,
                TreatmentSites = treatmentSites,
            });

            // The matching groups should be:
            // 1. Mild + Outpatient
            // 2. Moderate + Inpatient
            // 3. Severe + Inpatient
            // 4. Severe + Intensive Care Unit
            if (severities.ContainsValue(Severity.Severe))
            {
                if (!treatmentSites.ContainsValue(TreatmentSite.Inpatient) && !treatmentSites.ContainsValue(TreatmentSite.IntensiveCareUnit))
                {
                    const string msg = "Diagnosis return severe but the recommended treatment site is neither inpatient nor ICU";
                    return Result<SeverityDiagnosis>.Failure(new Error(ApplicationStatus.BusinessRuleViolation, msg));
                }

                if (treatmentSites.ContainsValue(TreatmentSite.IntensiveCareUnit))
                {
                    return Result<SeverityDiagnosis>.Success(
                        ApplicationStatus.Success,
                        new SeverityDiagnosis(
                            Severity.Severe,
                            TreatmentSite.IntensiveCareUnit,
                            metricsDiagnoses));
                }

                return Result<SeverityDiagnosis>.Success(
                    ApplicationStatus.Success,
                    new SeverityDiagnosis(
                        Severity.Severe,
                        TreatmentSite.Inpatient,
                        metricsDiagnoses));
            }
            else if (severities.ContainsValue(Severity.Moderate))
            {
                if (!treatmentSites.ContainsValue(TreatmentSite.Inpatient))
                {
                    const string msg = "Diagnosis return moderate but the recommended treatment site is not inpatient";
                    logger.LogWarning(msg);
                    return Result<SeverityDiagnosis>.Failure(new Error(ApplicationStatus.BusinessRuleViolation, msg));
                }

                return Result<SeverityDiagnosis>.Success(
                        ApplicationStatus.Success,
                        new SeverityDiagnosis(Severity.Moderate, TreatmentSite.Inpatient, metricsDiagnoses));
            }
            else if (severities.ContainsValue(Severity.Mild))
            {
                if (!treatmentSites.ContainsValue(TreatmentSite.Outpatient))
                {
                    const string msg = "Diagnosis return mild but the recommended treatment site is not outpatient";
                    logger.LogWarning(msg);
                    return Result<SeverityDiagnosis>.Failure(new Error(ApplicationStatus.BusinessRuleViolation, msg));
                }

                return Result<SeverityDiagnosis>.Success(
                    ApplicationStatus.Success,
                    new SeverityDiagnosis(
                        Severity.Mild,
                        TreatmentSite.Outpatient,
                        metricsDiagnoses));
            }

            throw new InvalidOperationException("Unexpected diagnosis result");
        }

        public Result<InfectionAssessment> AssessInfection(ClinicalContext context, IEnumerable<ClinicalObservation> observations, Severity severity, TreatmentSite treatmentSite)
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

            IEnumerable<HeavySuspected> heavySuspected = [];
            IEnumerable<Pathogen> worthSuspected = [];
            var evidences = new List<string>();

            // Step 1: Check for suspected pathogens
            var suspected = context.SuspectedCauses
                .Where(sc => sc.Severity == severity && sc.TreatmentSite == treatmentSite)
                .Select(sc => sc.Pathogen);

            // Step 2: Check for risk factors
            foreach (var pathogen in context.Pathogens)
            {
                var score = pathogen.RiskFactors.Count(r =>
                {
                    evidences.Add(BuildEvidence($"{pathogen.Name} - {r.Criterion.Name}", r.Criterion.Formula, observations));
                    return r.IsFactorSasified(observations);
                });
                logger.LogDebug("Risk factor calculate: score for {pathogen}: {score}", pathogen.Name, score);

                if (score == 0)
                {
                    continue;
                }

                // Check if this pathogen also exists in the suspected list
                if (suspected.Any(s => s.Id == pathogen.Id))
                {
                    // If yes, add a boost. The score counts satisfied risk factors,
                    // so +1 ranks a suspected pathogen above one with the same
                    // number of satisfied risk factors
                    logger.LogDebug("Pathogen {pathogen} is also in the suspected list, adding boost", pathogen.Name);
                    score++;
                }

                heavySuspected = heavySuspected.Append(new HeavySuspected(pathogen, score));
            }

            // Step 3: Assessment
            worthSuspected = suspected.Where(s => !heavySuspected.Select(hs => hs.Pathogen).Contains(s));

            return Result<InfectionAssessment>.Success(ApplicationStatus.Success, new InfectionAssessment
            {
                HeavySuspected = [.. heavySuspected.OrderByDescending(x => x.PriorityScore)],
                WorthSuspected = worthSuspected,
                Evidences = evidences,
            });
        }
    }
}
