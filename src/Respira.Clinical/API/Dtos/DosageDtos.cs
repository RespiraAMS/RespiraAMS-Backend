using Application.Features.Antibiotics.AddDosage;
using Application.Features.Antibiotics.UpdateDosage;
using Domain.Enums;
using Range = Domain.Models.Range;

namespace Respira.Clinical.API.Dtos;

public record AddDosageRequestDto
{
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
    /// The range of Glomerular Filtration Rate (GFR), used to adjust dose based on patient kidney
    /// </summary>
    public required Range? Crcl { get; set; }

    public AddDosageCommand ToCommand(Guid antibioticId)
    {
        return new AddDosageCommand()
        {
            AntibioticId = antibioticId,
            RouteOfAdministration = RouteOfAdministration,
            Dose = Dose,
            Crcl = Crcl,
        };
    }
}

public record UpdateDosageRequestDto
{
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
    /// The range of Glomerular Filtration Rate (GFR), used to adjust dose based on patient kidney. If null,
    /// this is the normal/standard dose
    /// </summary>
    public required Range? Crcl { get; set; }

    public UpdateDosageCommand ToCommand(Guid dosageId, Guid antibioticId)
    {
        return new UpdateDosageCommand
        {
            Id = dosageId,
            AntibioticId = antibioticId,
            RouteOfAdministration = RouteOfAdministration,
            Dose = Dose,
            Crcl = Crcl
        };
    }
}
