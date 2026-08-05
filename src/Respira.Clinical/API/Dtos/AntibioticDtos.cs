using Application.Features.Antibiotics.GetPagedAntibiotic;
using Application.Features.Antibiotics.UpdateAntibiotic;
using Application.Features.Antibiotics.UpdateAntibioticSpectrum;
using Domain.Enums;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Clinical.API.Dtos;

public class GetPagedAntibioticsRequestDto
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 10;
    public string? Name { get; set; }
    public Guid? AntibioticGroupId { get; set; }
    public AwareCategory? Category { get; set; }

    public GetPagedAntibioticQuery ToQuery()
    {
        return new GetPagedAntibioticQuery
        {
            Param = new PaginationParam()
            {
                Page = Page,
                Size = Size
            },
            Filter = new AntibioticFilter()
            {
                Name = Name,
                AntibioticGroupId = AntibioticGroupId,
                Category = Category
            }
        };
    }
}

public class UpdateAntibioticRequestDto
{
    public string Name { get; set; } = string.Empty;
    public Guid AntibioticGroupId { get; set; }
    public AwareCategory Category { get; set; }

    public UpdateAntibioticCommand ToCommand(Guid id)
    {
        return new UpdateAntibioticCommand
        {
            Id = id,
            Name = Name,
            AntibioticGroupId = AntibioticGroupId,
            Category = Category
        };
    }
}

public class UpdateAntibioticSpectrumRequestDto
{
    public List<Guid> PathogenIds { get; set; } = [];

    public UpdateAntibioticSpectrumCommand ToCommand(Guid id)
    {
        return new UpdateAntibioticSpectrumCommand
        {
            Id = id,
            PathogenIds = PathogenIds
        };
    }
}