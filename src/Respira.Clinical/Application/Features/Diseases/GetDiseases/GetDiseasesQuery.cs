namespace Application.Features.Diseases.GetDiseases;

public class GetDiseasesQuery : IQuery;

public class DiseaseItem
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}

public class GetDiseasesResult(IEnumerable<DiseaseItem> items)
{
    public IEnumerable<DiseaseItem> Diseases { get; set; } = items;
}