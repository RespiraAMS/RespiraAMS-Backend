using Microsoft.Extensions.Logging;
using Respira.Domain.Entities;
using Respira.Domain.Enums;
using Respira.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Domain.Services
{

    /// <summary>
    /// Diagnose service
    /// </summary>
    /// <param name="context">Clinical context</param>
    /// <param name="logger">Logger</param>
    public class DiagnoseService(ClinicalContext context, ILogger<DiagnoseService> logger) : IDiagnoseService
    {
        /// <summary>
        /// Calculate metrics score
        /// </summary>
        /// <param name="metrics">Metrics</param>
        /// <param name="observations">Clinical observations</param>
        /// <returns>Score result</returns>
        public decimal CalculateMetricsScore(ScoreMetrics metrics, IEnumerable<ClinicalObservation> observations)
        {
            return metrics.ScoringRules.Sum(sr =>
            {
                var score = sr.GetScore(observations);
                logger.LogDebug($"Calculated score for {metrics.Name}/{sr.Criterion.Name}: {score}");
                return score;
            });
        }

        /// <summary>
        /// CURB-65 severity diagnosis
        /// </summary>
        /// <param name="score">Score</param>
        /// <returns>Severity diagnosis</returns>
        /// <exception cref="InvalidOperationException">Throw if received invalid score</exception>
        public MetricsSeverityDiagnosis Curb65(int score, bool isBunMissing = false)
        {
            const string code = "CURB-65";
            // If BUN is missing (to be more exact, urea), then this would still be valid
            // (CRB-65, which has different value matching)
            if (isBunMissing)
            {
                return score switch
                {
                    < 0 => throw new ArgumentOutOfRangeException(nameof(score), "Score cannot be negative"),
                    0 => new MetricsSeverityDiagnosis(code, score, Severity.Mild, TreatmentSite.Outpatient),
                    1 or 2 => new MetricsSeverityDiagnosis(code, score, Severity.Moderate, TreatmentSite.Inpatient),
                    3 or 4 => new MetricsSeverityDiagnosis(code, score, Severity.Severe, TreatmentSite.IntensiveCareUnit),
                    _ => throw new InvalidOperationException($"Unexpected CURB-65 score: {score}"),
                };
            }

            // If BUN is present, then this would be CURB-65
            return score switch
            {
                < 0 => throw new ArgumentOutOfRangeException(nameof(score), "Score cannot be negative"),
                0 or 1 => new MetricsSeverityDiagnosis(code, score, Severity.Mild, TreatmentSite.Outpatient),
                2 => new MetricsSeverityDiagnosis(code, score, Severity.Moderate, TreatmentSite.Inpatient),
                >= 3 and <= 5 => new MetricsSeverityDiagnosis(code, score, Severity.Severe, TreatmentSite.IntensiveCareUnit),
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
        public MetricsSeverityDiagnosis Psi(int score)
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
                >= 0 and <= 70 => new MetricsSeverityDiagnosis(code, score, Severity.Mild, TreatmentSite.Outpatient),
                <= 90 => new MetricsSeverityDiagnosis(code, score, Severity.Moderate, TreatmentSite.Inpatient),
                <= 130 => new MetricsSeverityDiagnosis(code, score, Severity.Severe, TreatmentSite.Inpatient),
                _ => new MetricsSeverityDiagnosis(code, score, Severity.Severe, TreatmentSite.IntensiveCareUnit),
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
        public bool Ast(int score)
        {
            // AST metrics actually used to check if you need ICU or not
            // Since ICU case is actually severe already, we will return
            // Severe if true, while false would be the severity and 
            // treatment site passed in parameter
            // Note that a case can still be severe without ICU
            return score >= 3;
        }

        public Result<SeverityDiagnosis> DiagnoseSeverity(IEnumerable<ClinicalObservation> observations)
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
                    var isBunMissing = !observations.Any(x => x.Variable.Code.Equals("BUN"));
                    var diagnosis = Curb65((int)CalculateMetricsScore(metric, observations), isBunMissing);
                    metricsDiagnoses = metricsDiagnoses.Append(diagnosis);
                    severities.Add(metric.Code, diagnosis.Severity);
                    treatmentSites.Add(metric.Code, diagnosis.TreatmentSite);
                }
                else if (metric.Code.Equals("PSI"))
                {
                    var diagnosis = Psi((int)CalculateMetricsScore(metric, observations));
                    metricsDiagnoses = metricsDiagnoses.Append(diagnosis);
                    severities.Add(metric.Code, diagnosis.Severity);
                    treatmentSites.Add(metric.Code, diagnosis.TreatmentSite);
                }
                else if (metric.Code.Equals("IDSA/ATS"))
                {
                    var score = (int)CalculateMetricsScore(metric, observations);
                    var needIcu = Ast(score);
                    if (needIcu)
                    {
                        // If you need ICU, then the severity is obviously severe
                        metricsDiagnoses = metricsDiagnoses.Append(new MetricsSeverityDiagnosis(
                            metric.Code,
                            score,
                            Severity.Severe,
                            TreatmentSite.IntensiveCareUnit));
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

        public Result<InfectionProbability> InfectionProbability(Severity severity, TreatmentSite treatmentSite, IEnumerable<ClinicalObservation> observations)
        {
            IEnumerable<HeavySuspected> heavySuspected = [];
            IEnumerable<Pathogen> worthSuspected = context.SuspectedCauses
                .Where(sc => sc.Severity == severity && sc.TreatmentSite == treatmentSite)
                .Select(sc => sc.Pathogen);

            // Check for infection probability first
            foreach (var pathogen in context.Pathogens)
            {
                var total = pathogen.RiskFactors.Count();
                if (total == 0)
                {
                    logger.LogDebug("No risk factors for {pathogen}", pathogen.Name);
                    continue;
                }
                var matched = pathogen.RiskFactors.Count(r => r.Criterion.IsCriterionSatisfied(observations));
                var probability = (decimal)matched / total;
                logger.LogDebug("Risk factor calculate: probability for {pathogen}: {probability}", pathogen.Name, probability);
                heavySuspected = heavySuspected.Append(new HeavySuspected(pathogen, probability));
            }

            return Result<InfectionProbability>.Success(ApplicationStatus.Success, new InfectionProbability
            {
                HeavySuspected = heavySuspected,
                WorthSuspected = worthSuspected,
            });
        }
    }
}
