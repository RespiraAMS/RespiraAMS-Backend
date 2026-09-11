using Microsoft.Extensions.Logging;
using Respira.Domain.Entities;
using Respira.Domain.Enums;
using Respira.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Domain.Services
{
    public record SeverityDiagnosis(Severity Severity, TreatmentSite TreatmentSite);

    public class DiagnoseService(ClinicalContext context, ILogger<DiagnoseService> logger)
    {
        public decimal CalculateMetricsScore(ScoreMetrics metrics, IEnumerable<ClinicalObservation> observations)
        {
            return metrics.ScoringRules.Sum(sr =>
            {
                var score = sr.GetScore(observations);
                logger.LogDebug($"Calculated score for {metrics.Name}/{sr.Criterion.Name}: {score}");
                return score;
            });
        }

        public SeverityDiagnosis Curb65(int score)
        {
            return score switch
            {
                0 or 1 => new SeverityDiagnosis(Severity.Mild, TreatmentSite.Outpatient),
                2 => new SeverityDiagnosis(Severity.Moderate, TreatmentSite.Inpatient),
                > 2 => new SeverityDiagnosis(Severity.Severe, TreatmentSite.IntensiveCareUnit),
                _ => throw new InvalidOperationException($"Unexpected CURB-65 score: {score}"),
            };
        }

        public SeverityDiagnosis Psi(int score)
        {
            // PSI return a more detail classification with 5 levels, and
            // 4 type of treatment site: 
            // 1. I - II: Outpatient (score <= 70)
            // 2. III: Inpatient short term (71 <= score <= 90)
            // 3. IV: Inpatient long term (91 <= score <= 130)
            // 4. V: Intensive Care Unit (score > 130)
            // To match the result with the CURB-65 classification, we will simplify
            // the result to the following:
            // I - II: Mild + Outpatient
            // III + IV: Moderate + Inpatient
            // V: Severe + Intensive Care Unit
            return score switch
            {
                >= 0 and <= 70 => new SeverityDiagnosis(Severity.Mild, TreatmentSite.Outpatient),
                <= 130 => new SeverityDiagnosis(Severity.Moderate, TreatmentSite.Inpatient),
                _ => new SeverityDiagnosis(Severity.Severe, TreatmentSite.IntensiveCareUnit),
            };
        }

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

            foreach (var metric in context.Metrics)
            {
                if (metric.Code.Equals("CURB-65"))
                {
                    var diagnosis = Curb65((int)CalculateMetricsScore(metric, observations));
                    severities.Add(metric.Code, diagnosis.Severity);
                    treatmentSites.Add(metric.Code, diagnosis.TreatmentSite);
                }
                else if (metric.Code.Equals("PSI"))
                {
                    var diagnosis = Psi((int)CalculateMetricsScore(metric, observations));
                    severities.Add(metric.Code, diagnosis.Severity);
                    treatmentSites.Add(metric.Code, diagnosis.TreatmentSite);
                }
                else if (metric.Code.Equals("IDSA/ATS"))
                {
                    var needIcu = Ast((int)CalculateMetricsScore(metric, observations));
                    if (needIcu)
                    {
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
                        new SeverityDiagnosis(Severity.Severe, TreatmentSite.IntensiveCareUnit));
                }

                return Result<SeverityDiagnosis>.Success(
                    ApplicationStatus.Success,
                    new SeverityDiagnosis(Severity.Severe, TreatmentSite.Inpatient));
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
                        new SeverityDiagnosis(Severity.Moderate, TreatmentSite.Inpatient));
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
                    new SeverityDiagnosis(Severity.Mild, TreatmentSite.Outpatient));
            }

            throw new InvalidOperationException("Unexpected diagnosis result");
        }
    }
}
