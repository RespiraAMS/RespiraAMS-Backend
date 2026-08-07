using Application.Features.Antibiograms.GetPagedAntibiogram;
using Application.Features.Antibiograms.UpdateAntibiogram;
using Domain.Enums;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Clinical.API.Dtos;

public class GetPagedAntibiogramRequestDto
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
    /// <summary>
    /// Minimum Inhibitory Concentration (MIC) level
    /// </summary>
    public required MinimumInhibitoryConcentration MicLevel { get; set; }

    /// <summary>
    /// List of antibiotic IDs that corresponding to MIC level 
    /// </summary>
    public required List<Guid> MicIds { get; set; }

    /// <summary>
    /// List of antibiotic IDs that should be first prioritize when using for treatment 
    /// </summary>
    public required List<Guid> FirstPriorityMedicineIds { get; set; } = [];

    /// <summary>
    /// List of antibiotic IDs that should be secondary prioritize when using for treatment
    /// </summary>
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