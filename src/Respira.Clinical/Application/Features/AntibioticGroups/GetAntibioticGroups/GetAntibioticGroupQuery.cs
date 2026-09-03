namespace Application.Features.AntibioticGroups.GetAntibioticGroups;

public record GetAntibioticGroupsQuery : IQuery;

public record AntibioticGroupItem
{
    /// <summary>
    /// Antibiotic group ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Antibiotic group name
    /// </summary>
    public required string Name { get; set; }
}

public record GetAntibioticGroupsResult
{
    /// <summary>
    /// List of antibiotic group
    /// </summary>
    public required IEnumerable<AntibioticGroupItem> AntibioticGroups { get; set; }
}