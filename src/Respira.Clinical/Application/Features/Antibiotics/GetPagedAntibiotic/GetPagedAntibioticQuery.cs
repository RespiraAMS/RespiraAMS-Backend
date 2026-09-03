using Domain.Enums;

namespace Application.Features.Antibiotics.GetPagedAntibiotic;

public record AntibioticFilter
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
    public AwareClassification? Classification { get; set; }
}

public record GetPagedAntibioticQuery : IQuery
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

public record AntibioticGroupResult
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

public record PagedAntibioticItem
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
    public required AwareClassification Classification { get; set; }
}
