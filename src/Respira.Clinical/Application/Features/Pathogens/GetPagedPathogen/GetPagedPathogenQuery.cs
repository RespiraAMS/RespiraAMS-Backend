namespace Application.Features.Pathogens.GetPagedPathogen;

public class PathogenFilter
{
    public string? Name { get; set; }
}

public class GetPagedPathogenQuery : IQuery
{
    public required PaginationParam Param { get; set; } = null!;
    public PathogenFilter? Filter { get; set; }
}

public class GetPagedPathogenResult
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}