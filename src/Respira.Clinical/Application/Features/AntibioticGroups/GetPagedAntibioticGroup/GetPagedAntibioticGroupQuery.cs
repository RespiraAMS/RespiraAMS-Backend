namespace Application.Features.AntibioticGroups.GetPagedAntibioticGroup;

public record AntibioticGroupFilter
{
    /// <summary>
    /// Antibiotic group name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Antibiotic group parent ID
    /// </summary>
    public Guid? ParentId { get; set; }
}

public record GetPagedAntibioticGroupQuery : IQuery
{
    /// <summary>
    /// Pagination parameter
    /// </summary>
    public required PaginationParam Param { get; set; }

    /// <summary>
    /// Antibiotic group filter
    /// </summary>
    public AntibioticGroupFilter? Filter { get; set; }
}

public record PagedAntibioticGroupItem
{
    /// <summary>
    /// Antibiotic group ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Antibiotic group name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Antibiotic group parent ID
    /// </summary>
    public required Guid? ParentId { get; set; }

    /// <summary>
    /// Antibiotic group parent name
    /// </summary>
    public required string? ParentName { get; set; }
}