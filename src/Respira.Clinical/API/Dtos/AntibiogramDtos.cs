using Respira.Clinical.Application.Features.Antibiograms.GetPagedAntibiogram;
using Respira.Clinical.Application.Features.Antibiograms.UpdateAntibiogram;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.API.Dtos
{
    public record GetPagedAntibiogramRequestDto
    {
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
        public Guid? PathogenId { get; set; }
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
        public required Guid PathogenId { get; set; }
        public required MicCategory MicCategory { get; set; }
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
