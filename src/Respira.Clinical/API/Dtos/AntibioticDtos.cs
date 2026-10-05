using Respira.Clinical.Application.Features.Antibiotics.GetPagedAntibiotic;
using Respira.Clinical.Application.Features.Antibiotics.UpdateAntibiotic;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.API.Dtos
{
    public record GetPagedAntibioticsRequestDto
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
        /// Antibiotic name
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Antibiotic group ID
        /// </summary>
        public Guid? AntibioticGroupId { get; set; }

        /// <summary>
        /// Antibiotic WHO's AWaRe category
        /// </summary>
        public AwareClassification? Classification { get; set; }

        public GetPagedAntibioticQuery ToQuery()
        {
            return new GetPagedAntibioticQuery
            {
                Param = new PaginationParam
                {
                    Page = Page,
                    Size = Size
                },
                Filter = new AntibioticFilter
                {
                    Name = Name,
                    AntibioticGroupId = AntibioticGroupId,
                    Classification = Classification
                }
            };
        }
    }

    public record UpdateAntibioticRequestDto
    {
        /// <summary>
        /// Antibiotic name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Antibiotic group ID
        /// </summary>
        public required Guid AntibioticGroupId { get; set; }

        /// <summary>
        /// Antibiotic WHO's AWaRe category
        /// </summary>
        public required AwareClassification Classification { get; set; }

        public UpdateAntibioticCommand ToCommand(Guid id)
        {
            return new UpdateAntibioticCommand
            {
                Id = id,
                Name = Name,
                AntibioticGroupId = AntibioticGroupId,
                Classification = Classification
            };
        }
    }
}
