using Domain.Enums;
using Range = Domain.Models.Range;

namespace Application.Features.Antibiotics.GetAntibioticById;

public class GetAntibioticByIdQuery : IQuery
{
    public Guid Id { get; set; }
}

public class AntibioticGroupResult
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required Guid? ParentId { get; set; }
    public required string? ParentName { get; set; }
}

public class PathogenResult
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}

public class DosageResult
{
    public required Guid Id { get; set; }
    public required RouteOfAdministration RouteOfAdministration { get; set; }
    public required string Dose { get; set; }
    public required Range GlomerularFiltrationRate { get; set; }
}

public class AntibioticResult
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required AntibioticGroupResult AntibioticGroup { get; set; }
    public required AwareCategory Category { get; set; }
    public required List<PathogenResult> AntibioticSpectrum { get; set; }
    public required List<DosageResult> Dosages { get; set; }
}