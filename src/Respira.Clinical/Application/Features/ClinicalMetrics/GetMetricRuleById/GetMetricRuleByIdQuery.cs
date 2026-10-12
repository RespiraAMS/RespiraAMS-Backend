using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.GetMetricRuleById
{
    public record GetMetricRuleByIdQuery : IQuery
    {
        public required Guid Id { get; set; }
    }

    public record CriterionResult(Guid Id, string Name, string Formula);

    public record MetricRuleResult
    {
        public required Guid Id { get; set; }
        public required Guid ClinicalMetricId { get; set; }
        public required string ClinicalMetricName { get; set; }
        public required CriterionResult Criterion { get; set; }
        public string? ScoreFunction { get; set; }
        public bool? IsMajor { get; set; }
    }
}
