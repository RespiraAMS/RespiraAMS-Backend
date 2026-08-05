using Domain.Enums;

namespace Application.Features.Antibiotics.GetPagedAntibiotic;

public class AntibioticFilter
{
    /// <summary>
    /// Antibiotic name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Antibiotic group ID
    /// </summary>
    public Guid? AntibioticGroupId { get; set; }

    /// <summary>
    /// Antibiotic AWaRe category
    /// </summary>
    public AwareCategory? Category { get; set; }
}

public class GetPagedAntibioticQuery : IQuery
{
    /// <summary>
    /// Pagination parameter
    /// </summary>
    public required PaginationParam Param { get; set; }

    /// <summary>
    /// Antibiotic filter
    /// </summary>
    public AntibioticFilter? Filter { get; set; }
}

public class AntibioticGroupResult
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

public class PagedAntibioticItem
{
    /// <summary>
    /// Antibiotic ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Antibiotic name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Antibiotic group
    /// </summary>
    public required AntibioticGroupResult AntibioticGroup { get; set; }

    /// <summary>
    /// Antibiotic WHO's AWaRe category
    /// </summary>
    public required AwareCategory Category { get; set; }
}