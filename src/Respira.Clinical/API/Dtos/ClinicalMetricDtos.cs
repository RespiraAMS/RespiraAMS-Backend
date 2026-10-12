using Respira.Clinical.Application.Features.ClinicalMetrics.GetPagedClinicalMetric;
using Respira.Clinical.Application.Features.ClinicalMetrics.UpdateClinicalMetric;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.API.Dtos
{
    public record GetPagedClinicalMetricRequestDto
    {
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;

        public string? Code { get; set; }
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
                    Code = Code,
                    IsApplied = IsApplied
                }
            };
        }
    }

    public record UpdateClinicalMetricRequestDto
    {
        public required string Name { get; set; }
        public required string Code { get; set; }
        public required string Description { get; set; }
        public required string Version { get; set; }
        public required string Publisher { get; set; }
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
