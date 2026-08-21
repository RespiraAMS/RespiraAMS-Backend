using Domain.Enums;
using Range = Domain.Models.Range;

namespace Application.Features.Antibiotics.AddDosage;

public class AddDosageCommand : ICommand
{
    /// <summary>
    /// Antibiotic ID
    /// </summary>
    public required Guid AntibioticId { get; set; }

    /// <summary>
    /// Route of administration
    /// </summary>
    public required RouteOfAdministration RouteOfAdministration { get; set; }

    /// <summary>
    /// Antibiotic dosage. There is no exact format, or content rule for dosage
    /// </summary>
    /// <example>500 mg/day</example>
    public required string Dose { get; set; }

    /// <summary>
    /// The range of Glomerular Filtration Rate (GFR), used to adjust dose based on patient kidney.
    /// If null, this is the standard dose
    /// </summary>
    public required Range? Crcl { get; set; }
}

public class AddDosageResult(Guid id)
{
    /// <summary>
    /// Dosage ID
    /// </summary>
    public Guid Id { get; set; } = id;
}
