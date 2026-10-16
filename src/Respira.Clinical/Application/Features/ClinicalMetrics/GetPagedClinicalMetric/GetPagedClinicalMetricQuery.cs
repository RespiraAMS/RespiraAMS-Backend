using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.GetPagedClinicalMetric
{
    public record ClinicalMetricFilter
    {
        /// <summary>
        /// Is clinical metric applied
        /// </summary>
        public bool? IsApplied { get; set; }
    }

    public record GetPagedClinicalMetricQuery : IQuery
    {
        /// <summary>
        /// Pagination parameter
        /// </summary>
        public required PaginationParam Param { get; set; }

        /// <summary>
        /// Clinical metric filter
        /// </summary>
        public ClinicalMetricFilter? Filter { get; set; }
    }

    public record PagedClinicalMetricItem
    {
        /// <summary>
        /// Clinical metric ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Clinical metric name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Clinical metric code
        /// </summary>
        public required string Code { get; set; }

        /// <summary>
        /// Clinical metric description
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Clinical metric version
        /// </summary>
        public required string Version { get; set; }

        /// <summary>
        /// Clinical metric publisher
        /// </summary>
        public required string Publisher { get; set; }

        /// <summary>
        /// Is clinical metric applied
        /// </summary>
        public required bool IsApplied { get; set; }
    }
}
