using Domain.Enums;

namespace Application.Features.Antibiotics.GetPagedAntibiotic;

public class AntibioticFilter
{
    public string? Name { get; set; }
    public Guid? AntibioticGroupId { get; set; }
    public AwareCategory? Category { get; set; }
}

public class GetPagedAntibioticQuery : IQuery
{
    public required PaginationParam Param { get; set; }
    public AntibioticFilter? Filter { get; set; }
}

public class AntibioticGroupResult
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}

public class PagedAntibioticItem
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required AntibioticGroupResult AntibioticGroup { get; set; }
    public required AwareCategory Category { get; set; }
}