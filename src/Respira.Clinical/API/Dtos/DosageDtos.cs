using Application.Features.Antibiotics.AddDosage;
using Application.Features.Antibiotics.UpdateDosage;
using Domain.Enums;
using Range = Domain.Models.Range;

namespace Respira.Clinical.API.Dtos;

public class AddDosageRequestDto
{
    public RouteOfAdministration RouteOfAdministration { get; set; }
    public string Dose { get; set; } = string.Empty;
    public Range GlomerularFiltrationRate { get; set; } = null!;

    public AddDosageCommand ToCommand(Guid antibioticId)
    {
        return new AddDosageCommand()
        {
            AntibioticId = antibioticId,
            RouteOfAdministration = RouteOfAdministration,
            Dose = Dose,
            GlomerularFiltrationRate = GlomerularFiltrationRate,
        };
    }
}

public class UpdateDosageRequestDto
{
    public RouteOfAdministration RouteOfAdministration { get; set; }
    public string Dose { get; set; } = string.Empty;
    public Range GlomerularFiltrationRate { get; set; } = null!;

    public UpdateDosageCommand ToCommand(Guid dosageId)
    {
        return new UpdateDosageCommand
        {
            Id = dosageId,
            RouteOfAdministration = RouteOfAdministration,
            Dose = Dose,
            GlomerularFiltrationRate = GlomerularFiltrationRate
        };
    }
}