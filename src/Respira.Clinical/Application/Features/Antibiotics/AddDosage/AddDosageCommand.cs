using Domain.Enums;
using Range = Domain.Models.Range;

namespace Application.Features.Antibiotics.AddDosage;

public class AddDosageCommand : ICommand
{
    public required Guid AntibioticId { get; set; }
    public required RouteOfAdministration RouteOfAdministration { get; set; }
    public required string Dose { get; set; }
    public required Range GlomerularFiltrationRate { get; set; }
}

public class AddDosageResult(Guid id)
{
    public Guid Id { get; set; } = id;
}