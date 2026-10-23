using Respira.Clinical.Domain.Enums;

namespace Respira.Clinical.Domain.Models
{
    public record MetricsSeverityDiagnosis
    {
        public required string Code { get; set; }
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
        public required IEnumerable<Evidence> Evidences { get; set; }
    }

    /// <summary>
    /// Individual severity diagnosis for a score metrics
    /// </summary>
    public record ScoreMetricsSeverityDiagnosis : MetricsSeverityDiagnosis
    {
        public required int Score { get; set; }
    }

    /// <summary>
    /// Individual severity diagnosis for a major/minor metrics
    /// </summary>
    public record MajorMinorMetricsSeverityDiagnosis : MetricsSeverityDiagnosis
    {
        public required int MajorMatched { get; set; }
        public required int MinorMatched { get; set; }
    }
}
