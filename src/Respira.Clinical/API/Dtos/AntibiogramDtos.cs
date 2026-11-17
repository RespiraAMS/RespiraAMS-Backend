using Respira.Clinical.Application.Features.Antibiograms.GetPagedAntibiogram;
using Respira.Clinical.Application.Features.Antibiograms.UpdateAntibiogram;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.API.Dtos
{
    public record GetPagedAntibiogramRequestDto
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
        /// Pathogen ID
        /// </summary>
        public Guid? PathogenId { get; set; }

        /// <summary>
        /// MIC category
        /// </summary>
        public MicCategory? MicCategory { get; set; }
        public GetPagedAntibiogramQuery ToQuery()
        {
            return new GetPagedAntibiogramQuery
            {
                Param = new PaginationParam
                {
                    Page = Page,
                    Size = Size
                },
                Filter = new AntibiogramFilter
                {
                    PathogenId = PathogenId,
                    MicCategory = MicCategory
                }
            };
        }
    }

    public record UpdateAntibiogramRequestDto
    {
        /// <summary>
        /// MIC category
        /// </summary>
        public required MicCategory MicCategory { get; set; }

        /// <summary>
        /// Pathogen ID
        /// </summary>
        public required Guid PathogenId { get; set; }

        /// <summary>
        /// List of antibiotics used to treat this pathogen.
        /// Note that, this list is a OR semantic.
        /// </summary>
        public required List<Guid> AntibioticIds { get; set; }

        public UpdateAntibiogramCommand ToCommand(Guid id)
        {
            return new UpdateAntibiogramCommand
            {
                Id = id,
                PathogenId = PathogenId,
                MicCategory = MicCategory,
                AntibioticIds = AntibioticIds
            };
        }
    }
}
