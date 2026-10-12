using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.GetPagedClinicalMetric
{
    public record ClinicalMetricFilter
    {
        public string? Code { get; set; }
        public bool? IsApplied { get; set; }
    }

    public record GetPagedClinicalMetricQuery : IQuery
    {
        public required PaginationParam Param { get; set; }
        public ClinicalMetricFilter? Filter { get; set; }
    }

    public record PagedClinicalMetricItem
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Code { get; set; }
        public required string Description { get; set; }
        public required string Version { get; set; }
        public required string Publisher { get; set; }
        public required bool IsApplied { get; set; }
    }
}
