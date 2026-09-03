namespace Application.Features.Pathogens.GetPathogens;

public record GetPathogensQuery : IQuery;

public record PathogenItem
{
    /// <summary>
    /// Pathogen ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Pathogen name
    /// </summary>
    public required string Name { get; set; }
}

public record GetPathogensResult
{
    /// <summary>
    /// List of pathogens
    /// </summary>
    public required IEnumerable<PathogenItem> Pathogens { get; set; }
}