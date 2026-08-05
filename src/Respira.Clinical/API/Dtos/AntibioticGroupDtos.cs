using Application.Features.AntibioticGroups.GetPagedAntibioticGroup;
using Application.Features.AntibioticGroups.UpdateAntibioticGroup;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Clinical.API.Dtos;

public class GetPagedAntibioticGroupRequestDto
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
    /// Antibiotic group name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Antibiotic group parent ID
    /// </summary>
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
    /// <summary>
    /// Antibiotic group name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Antibiotic group description
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Antibiotic group parent ID
    /// </summary>
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