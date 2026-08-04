using Application.Features.Antibiograms.GetPagedAntibiogram;
using Application.Features.Antibiograms.UpdateAntibiogram;
using Domain.Enums;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Clinical.API.Dtos;

public class GetPagedAntibiogramRequestDto
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 10;
    public Guid? PathogenId { get; set; }

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
                PathogenId = PathogenId
            }
        };
    }
}

public class UpdateAntibiogramRequestDto
{
    public required MinimumInhibitoryConcentration MicLevel { get; set; }
    public required List<Guid> MicIds { get; set; }
    public required List<Guid> FirstPriorityMedicineIds { get; set; } = [];
    public required List<Guid> SecondPriorityMedicineIds { get; set; } = [];

    public UpdateAntibiogramCommand ToCommand(Guid id)
    {
        return new UpdateAntibiogramCommand
        {
            Id = id,
            MicLevel = MicLevel,
            MicIds = MicIds,
            FirstPriorityMedicineIds = FirstPriorityMedicineIds,
            SecondPriorityMedicineIds = SecondPriorityMedicineIds
        };
    }
}