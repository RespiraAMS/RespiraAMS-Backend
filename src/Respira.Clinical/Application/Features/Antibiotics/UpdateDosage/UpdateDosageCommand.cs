using Domain.Enums;
using Range = Domain.Models.Range;

namespace Application.Features.Antibiotics.UpdateDosage;

public class UpdateDosageCommand : ICommand
{
    /// <summary>
    /// Dosage ID
    /// </summary>
    public required Guid Id { get; set; }


    /// <summary>
    /// Antibiotic ID
    /// </summary>
    public required Guid AntibioticId { get; set; }

    /// <summary>
    /// Antibiotic dosage. There is no exact format, or content rule for dosage
    /// </summary>
    public required RouteOfAdministration RouteOfAdministration { get; set; }

    /// <summary>
    /// Dosage
    /// </summary>
    /// <example>500 mg/day</example>
    public required string Dose { get; set; }

    /// <summary>
    /// The range of Glomerular Filtration Rate (GFR), used to adjust dose based on patient kidney.
    /// If null, this is the standard dose
    /// </summary>
    public required Range? Crcl { get; set; }
}
