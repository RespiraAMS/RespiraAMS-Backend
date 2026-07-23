namespace Application.Features.AntibioticGroups.GetAntibioticGroups;

public class GetAntibioticGroupQuery : IQuery;

public class GetAntibioticGroupResult
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}