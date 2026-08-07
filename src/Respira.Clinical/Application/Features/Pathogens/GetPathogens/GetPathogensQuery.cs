namespace Application.Features.Pathogens.GetPathogens;

public class GetPathogensQuery : IQuery;

public class PathogenItem
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

public class GetPathogensResult(IEnumerable<PathogenItem> pathogens)
{
    /// <summary>
    /// List of pathogens
    /// </summary>
    public IEnumerable<PathogenItem> Pathogens { get; set; } = pathogens;
}