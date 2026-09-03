namespace Application.Features.Diseases.GetDiseases;

public record GetDiseasesQuery : IQuery;

public record DiseaseItem
{
    /// <summary>
    /// Disease ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Disease name
    /// </summary>
    public required string Name { get; set; }
}

public record GetDiseasesResult
{
    /// <summary>
    /// List of diseases
    /// </summary>
    public required IEnumerable<DiseaseItem> Diseases { get; set; }
}