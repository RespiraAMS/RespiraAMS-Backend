namespace Application.Features.Pathogens.GetPathogens;

public class GetPathogensQuery : IQuery;

public class GetPathogensResult
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}