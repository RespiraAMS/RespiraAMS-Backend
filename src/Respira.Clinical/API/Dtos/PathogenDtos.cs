using Application.Features.AntibioticGroups.GetPagedAntibioticGroup;
using Application.Features.Pathogens.GetPagedPathogen;
using Application.Features.Pathogens.UpdatePathogen;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Clinical.API.Dtos;

public class GetPagedPathogenRequestDto
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 10;
    public string? Name { get; set; }

    public GetPagedPathogenQuery ToQuery()
    {
        return new GetPagedPathogenQuery()
        {
            Param = new PaginationParam()
            {
                Page = Page,
                Size = Size,
            },
            Filter = new PathogenFilter()
            {
                Name = Name,
            }
        };
    }
}

public class UpdatePathogenRequestDto
{
    public required string Name { get; set; }
    public required string Description { get; set; }

    public UpdatePathogenCommand ToCommand(Guid id)
    {
        return new UpdatePathogenCommand
        {
            Id = id,
            Name = Name,
            Description = Description,
        };

    }
}