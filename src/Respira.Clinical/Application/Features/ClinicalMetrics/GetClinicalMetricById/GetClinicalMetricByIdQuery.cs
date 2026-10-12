using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.GetClinicalMetricById
{
    public record GetClinicalMetricByIdQuery : IQuery
    {
        public required Guid Id { get; set; }
    }

    public record CriterionResult(Guid Id, string Name, string Formula);

    public record MetricRuleResult
    {
        public required Guid Id { get; set; }
        public required CriterionResult Criterion { get; set; }
        public string? ScoreFunction { get; set; }
        public bool? IsMajorRule { get; set; }
    }

    public record ClinicalMetricResult
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Code { get; set; }
        public required string Description { get; set; }
        public required string Version { get; set; }
        public required string Publisher { get; set; }
        public bool IsApplied { get; set; }
        public IEnumerable<MetricRuleResult> Rules { get; set; } = [];
    }
}
