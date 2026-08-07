namespace Application.Features.Diseases.GetDiseases;

public class GetDiseasesQuery : IQuery;

public class DiseaseItem
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

public class GetDiseasesResult(IEnumerable<DiseaseItem> items)
{
    /// <summary>
    /// List of diseases
    /// </summary>
    public IEnumerable<DiseaseItem> Diseases { get; set; } = items;
}