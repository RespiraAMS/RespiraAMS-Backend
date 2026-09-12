using Respira.Domain.Enums;

namespace Respira.Domain.Models
{
    /// <summary>
    /// Individual severity diagnosis for a metrics
    /// </summary>
    /// <param name="Code">Metrics code (CURB-65, IDSA/ATS, etc.)</param>
    /// <param name="Score">Metrics score</param>
    /// <param name="Severity">Diagnosis severity</param>
    /// <param name="TreatmentSite">Diagnosis treatment site</param>
    public record MetricsSeverityDiagnosis(string Code, int Score, Severity Severity, TreatmentSite TreatmentSite);

    /// <summary>
    /// Final severity diagnosis, after combining all metrics results.
    /// This record also include all the individual metrics diagnosis results
    /// for audit to why the final result is the way it is
    /// </summary>
    /// <param name="Severity">Final severity diagnosis</param>
    /// <param name="TreatmentSite">Final treatment site diagnosis</param>
    /// <param name="MetricsDiagnoses">List of alll individual metrics diagnosis results</param>
    public record SeverityDiagnosis(Severity Severity, TreatmentSite TreatmentSite, IEnumerable<MetricsSeverityDiagnosis> MetricsDiagnoses);
}
