using Respira.Clinical.Domain.Enums;

namespace Respira.Clinical.Domain.Models
{
    /// <summary>
    /// Individual severity diagnosis for a score metrics
    /// </summary>
    public record ScoreMetricsSeverityDiagnosis
    {
        public required string Code { get; set; }
        public required int Score { get; set; }
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
        public required List<string> Evidences { get; set; }
    }

    /// <summary>
    /// Individual severity diagnosis for a major/minor metrics
    /// </summary>
    public record MajorMinorMetricsSeverityDiagnosis
    {
        public required string Code { get; set; }
        public required int MajorMatched { get; set; }
        public required int MinorMatched { get; set; }
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
        public required List<string> Evidences { get; set; }
    }
}
