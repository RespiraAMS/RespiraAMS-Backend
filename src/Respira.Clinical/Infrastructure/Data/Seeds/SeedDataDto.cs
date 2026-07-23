using Domain.Enums;

namespace Infrastructure.Data.Seeds;

public record SeedDataDto
{
    public required List<AntibioticGroupDto> AntibioticGroups { get; init; }
    public required List<PathogenDto> Pathogens { get; init; }
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