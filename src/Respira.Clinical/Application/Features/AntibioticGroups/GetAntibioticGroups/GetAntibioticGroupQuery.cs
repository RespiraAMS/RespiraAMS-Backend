namespace Application.Features.AntibioticGroups.GetAntibioticGroups;

public class GetAntibioticGroupsQuery : IQuery;

public class AntibioticGroupItem
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

public class GetAntibioticGroupsResult(IEnumerable<AntibioticGroupItem> groups)
{
    /// <summary>
    /// List of antibiotic group
    /// </summary>
    public IEnumerable<AntibioticGroupItem> AntibioticGroups { get; set; } = groups;
}