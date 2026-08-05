namespace Application.Features.Diseases.GetPagedDisease;

public class DiseaseFilter
{
    public string? Name { get; set; }
}

public class GetPagedDiseaseQuery : IQuery
{
    public required PaginationParam Param { get; set; }
    public DiseaseFilter? Filter { get; set; }
}

public class PagedDiseaseItem
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}