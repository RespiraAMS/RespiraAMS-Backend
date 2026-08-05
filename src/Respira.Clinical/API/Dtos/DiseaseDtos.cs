using Application.Features.Diseases.GetPagedDisease;
using Application.Features.Diseases.UpdateDisease;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Clinical.API.Dtos;

public class GetPagedDiseaseRequestDto
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 10;
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
    public required string Name { get; set; }
    public required string Description { get; set; }
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