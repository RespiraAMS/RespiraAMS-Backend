using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.SearchClinicalMetric
{
    public record SearchClinicalMetricQuery : IQuery
    {
        public required PaginationParam Param { get; set; }
        public required string SearchTerm { get; set; }
    }

    public record SearchClinicalMetricItem
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

        /// <summary>
        /// Search rank (most relevant to search term)
        /// </summary>
        public required float Rank { get; set; }
    }
}
