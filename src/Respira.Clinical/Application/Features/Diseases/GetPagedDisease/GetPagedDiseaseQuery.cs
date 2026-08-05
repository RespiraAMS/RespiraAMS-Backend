namespace Application.Features.Diseases.GetPagedDisease;

public class DiseaseFilter
{
    /// <summary>
    /// Disease name
    /// </summary>
    public string? Name { get; set; }
}

public class GetPagedDiseaseQuery : IQuery
{
    /// <summary>
    /// Pagination parameter
    /// </summary>
    public required PaginationParam Param { get; set; }

    /// <summary>
    /// Disease filter
    /// </summary>
    public DiseaseFilter? Filter { get; set; }
}

public class PagedDiseaseItem
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