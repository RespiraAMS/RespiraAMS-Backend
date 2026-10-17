using Respira.Clinical.Application.Features.ClinicalMetrics.GetPagedClinicalMetric;
using Respira.Clinical.Application.Features.ClinicalMetrics.SearchClinicalMetric;
using Respira.Clinical.Application.Features.ClinicalMetrics.UpdateClinicalMetric;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.API.Dtos
{
    public record GetPagedClinicalMetricRequestDto
    {
        /// <summary>
        /// Pagination parameter: page index (1-based)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Pagination parameter: page size
        /// </summary>
        public int Size { get; set; } = 10;

        /// <summary>
        /// Is clinical metric applied
        /// </summary>
        public bool? IsApplied { get; set; }

        public GetPagedClinicalMetricQuery ToQuery()
        {
            return new GetPagedClinicalMetricQuery
            {
                Param = new PaginationParam
                {
                    Page = Page,
                    Size = Size
                },
                Filter = new ClinicalMetricFilter
                {
                    IsApplied = IsApplied
                }
            };
        }
    }

    public record SearchClinicalMetricRequestDto
    {
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
        public string SearchTerm { get; set; } = string.Empty;

        public SearchClinicalMetricQuery ToQuery()
        {
            return new SearchClinicalMetricQuery
            {
                Param = new PaginationParam
                {
                    Page = Page,
                    Size = Size
                },
                SearchTerm = SearchTerm
            };
        }
    }

    public record UpdateClinicalMetricRequestDto
    {
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
        public bool IsApplied { get; set; }

        public UpdateClinicalMetricCommand ToCommand(Guid id)
        {
            return new UpdateClinicalMetricCommand
            {
                Id = id,
                Name = Name,
                Code = Code,
                Description = Description,
                Version = Version,
                Publisher = Publisher,
                IsApplied = IsApplied
            };
        }
    }
}
