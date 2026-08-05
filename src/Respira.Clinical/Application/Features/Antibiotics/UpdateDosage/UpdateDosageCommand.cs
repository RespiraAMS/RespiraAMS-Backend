using Domain.Enums;
using Range = Domain.Models.Range;

namespace Application.Features.Antibiotics.UpdateDosage;

public class UpdateDosageCommand : ICommand
{
    public required Guid Id { get; set; }
    public required RouteOfAdministration RouteOfAdministration { get; set; }
    public required string Dose { get; set; }
    public required Range GlomerularFiltrationRate { get; set; }
}