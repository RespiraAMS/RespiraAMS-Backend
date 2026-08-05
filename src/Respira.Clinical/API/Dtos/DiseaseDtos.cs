using Application.Features.Diseases.GetPagedDisease;
using Application.Features.Diseases.UpdateDisease;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Clinical.API.Dtos;

public class GetPagedDiseaseRequestDto
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
    /// Disease name
    /// </summary>
    public string? Name { get; set; }

    public GetPagedDiseaseQuery ToQuery()
    {
        return new GetPagedDiseaseQuery
        {
            Param = new PaginationParam()
            {
                Page = Page,
                Size = Size
            },
            Filter = new DiseaseFilter()
            {
                Name = Name
            }
        };
    }
}

public class UpdateDiseaseRequestDto
{
    /// <summary>
    /// Disease name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Disease description
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Disease's ICU score minimum threshold (&gt;= threshold) to consider needing ICU hospitalization 
    /// </summary>
    public required int IcuScoreThreshold { get; set; }

    public UpdateDiseaseCommand ToCommand(Guid id)
    {
        return new UpdateDiseaseCommand
        {
            Id = id,
            Name = Name,
            Description = Description,
            IcuScoreThreshold = IcuScoreThreshold
        };
    }
}