using Domain.Enums;
using Range = Domain.Models.Range;

namespace Application.Features.Antibiotics.GetAntibioticById;

public class GetAntibioticByIdQuery : IQuery
{
    /// <summary>
    /// Antibiotic ID
    /// </summary>
    public Guid Id { get; set; }
}

public class AntibioticGroupResult
{
    /// <summary>
    /// Antibiotic group ID
    /// </summary>
    public required Guid Id { get; set; }

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
    public required Guid? ParentId { get; set; }

    /// <summary>
    /// Antibiotic group parent name
    /// </summary>
    public required string? ParentName { get; set; }
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

public class DosageResult
{
    /// <summary>
    /// Dosage ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Dosage's route of administration
    /// </summary>
    public required RouteOfAdministration RouteOfAdministration { get; set; }

    /// <summary>
    /// Dosage
    /// </summary>
    public required string Dose { get; set; }

    /// <summary>
    /// Glomerular Filtration Rate. If null, this is the standard dose
    /// </summary>
    public required Range? GlomerularFiltrationRate { get; set; }
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

    /// <summary>
    /// Antibiotic group
    /// </summary>
    public required AntibioticGroupResult AntibioticGroup { get; set; }

    /// <summary>
    /// Antibiotic WHO's AWaRe category
    /// </summary>
    public required AwareClassification Classification { get; set; }

    /// <summary>
    /// Antibiotic spectrum: list of pathogen that this antibiotic can theoretically affect
    /// </summary>
    public required List<PathogenResult> AntibioticSpectrum { get; set; }

    /// <summary>
    /// Antibiotic dosages
    /// </summary>
    public required List<DosageResult> Dosages { get; set; }
}
