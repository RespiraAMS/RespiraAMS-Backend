namespace Application.Features.Pathogens.GetPathogens;

public class GetPathogensQuery : IQuery;

public class PathogenItem
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}

public class GetPathogensResult(IEnumerable<PathogenItem> pathogens)
{
    public IEnumerable<PathogenItem> Pathogens { get; set; } = pathogens;
}