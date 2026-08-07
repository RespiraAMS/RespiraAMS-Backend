using Application.Features.Antibiotics.GetPagedAntibiotic;
using Application.Features.Antibiotics.UpdateAntibiotic;
using Application.Features.Antibiotics.UpdateAntibioticSpectrum;
using Domain.Enums;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Clinical.API.Dtos;

public class GetPagedAntibioticsRequestDto
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
    public required AwareCategory Category { get; set; }

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
    /// <summary>
    /// List of pathogen IDs
    /// </summary>
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