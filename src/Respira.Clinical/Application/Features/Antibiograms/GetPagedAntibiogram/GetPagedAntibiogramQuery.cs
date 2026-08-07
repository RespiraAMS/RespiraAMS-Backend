using Domain.Enums;

namespace Application.Features.Antibiograms.GetPagedAntibiogram;

public class AntibiogramFilter
{
    /// <summary>
    /// Pathogen ID
    /// </summary>
    public Guid? PathogenId { get; set; }
}

public class GetPagedAntibiogramQuery : IQuery
{
    /// <summary>
    /// Pagination parameter
    /// </summary>
    public required PaginationParam Param { get; set; }

    /// <summary>
    /// Antibiogram filter
    /// </summary>
    public AntibiogramFilter? Filter { get; set; }
}

public class PathogenResult
{
    /// <summary>
    /// Pathogen ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Pathogen name
    /// </summary>
    public required string Name { get; set; }
}

public class AntibioticResult
{
    /// <summary>
    /// Antibiotic ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Antibiotic name
    /// </summary>
    public required string Name { get; set; }
}

public class PagedAntibiogramItem
{
    /// <summary>
    /// Antibiogram ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Pathogen assigned to this antibiogram
    /// </summary>
    public required PathogenResult Pathogen { get; set; }

    /// <summary>
    /// Minimum Inhibitory Concentration (MIC) level
    /// </summary>
    public required MinimumInhibitoryConcentration MicLevel { get; set; }

    /// <summary>
    /// The list of all antibiotics with this MIC level
    /// </summary>
    public required List<AntibioticResult> Mics { get; set; }

    /// <summary>
    /// The list of first prioritized medicines
    /// </summary>
    public required List<AntibioticResult> FirstPriorityMedicines { get; set; }

    /// <summary>
    /// The list of secondary prioritized medicines
    /// </summary>
    public required List<AntibioticResult> SecondPriorityMedicines { get; set; }
}