using Application.Features.AntibioticGroups.GetPagedAntibioticGroup;
using Application.Features.AntibioticGroups.UpdateAntibioticGroup;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Clinical.API.Dtos;

public class GetPagedAntibioticGroupRequestDto
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 10;
    public string? Name { get; set; }
    public Guid? ParentId { get; set; }

    public GetPagedAntibioticGroupQuery ToQuery()
    {
        return new GetPagedAntibioticGroupQuery
        {
            Param = new PaginationParam()
            {
                Page = Page,
                Size = Size,
            },
            Filter = new AntibioticGroupFilter()
            {
                Name = Name,
                ParentId = ParentId
            }
        };
    }
}

public class UpdateAntibioticGroupRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }

    public UpdateAntibioticGroupCommand ToCommand(Guid id)
    {
        return new UpdateAntibioticGroupCommand
        {
            Id = id,
            Name = Name,
            Description = Description,
            ParentId = ParentId
        };

    }
}