namespace Application.Features.AntibioticGroups.GetAntibioticGroups;

public class GetAntibioticGroupsQuery : IQuery;

public class AntibioticGroupItem
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}

public class GetAntibioticGroupsResult(IEnumerable<AntibioticGroupItem> groups)
{
    public IEnumerable<AntibioticGroupItem> AntibioticGroups { get; set; } = groups;
}