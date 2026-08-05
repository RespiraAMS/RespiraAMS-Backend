namespace Application.Features.Pathogens.GetPagedPathogen;

public class PathogenFilter
{
    /// <summary>
    /// Pathogen name
    /// </summary>
    public string? Name { get; set; }
}

public class GetPagedPathogenQuery : IQuery
{
    /// <summary>
    /// Pagination param
    /// </summary>
    public required PaginationParam Param { get; set; } = null!;

    /// <summary>
    /// Pathogen filter
    /// </summary>
    public PathogenFilter? Filter { get; set; }
}

public class PagedPathogenItem
{
    /// <summary>
    /// Pathogen ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Pathogen name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Pathogen description
    /// </summary>
    public required string Description { get; set; }
}