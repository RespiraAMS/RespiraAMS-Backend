using Domain.Enums;

namespace Infrastructure.Data.Seeds;

public record SeedDataDto
{
    public required List<AntibioticGroupDto> AntibioticGroups { get; init; }
    public required List<PathogenDto> Pathogens { get; init; }
    public required List<AntibioticDto> Antibiotics { get; init; }
}

public record AntibioticGroupDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public Guid? ParentId { get; init; }
}

public record PathogenDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
}

public record AntibioticDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Guid AntibioticGroupId { get; init; }
    public required AwareCategory Category { get; init; }
    public List<Guid> PathogenIds { get; init; } = [];
    public required List<DosageDto> Dosages { get; init; }
}

public record DosageDto
{
    public required Guid Id { get; init; }
    public required RouteOfAdministration RouteOfAdministration { get; init; }
    public required string Dose { get; init; }
    public required RangeDto GlomerularFiltrationRate { get; init; }
}

public record RangeDto
{
    public required decimal Min { get; init; }
    public required bool IsMinExclusive { get; init; }
    public required decimal Max { get; init; }
    public required bool IsMaxExclusive { get; init; }
    public string? Unit { get; init; }
}