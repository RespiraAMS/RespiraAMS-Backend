namespace Application.Features.AntibioticGroups.GetPagedAntibioticGroup;

public class AntibioticGroupFilter
{
    public string? Name { get; set; }
    public Guid? ParentId { get; set; }
}

public class GetPagedAntibioticGroupQuery : IQuery
{
    public required PaginationParam Param { get; set; }
    public AntibioticGroupFilter? Filter { get; set; }
}

public class GetPagedAntibioticGroupResult
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required Guid? ParentId { get; set; }
    public required string? ParentName { get; set; }
}