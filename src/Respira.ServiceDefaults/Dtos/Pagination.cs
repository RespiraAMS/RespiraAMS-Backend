namespace Respira.ServiceDefaults.Dtos;

/// <summary>
/// Pagination query parameters
/// </summary>
public class PaginationParam
{
    /// <summary>
    /// Page index, (1-indexed, default to 1)
    /// </summary>
    public int Page { get; set; } = 1;
    /// <summary>
    /// Page size (default to 10)
    /// </summary>
    public int Size { get; set; } = 10;
}

/// <summary>
/// Pagination metadata
/// </summary>
public class PaginationMetadata
{
    /// <summary>
    /// Boolean flag: true if there is next page
    /// </summary>
    public bool HasNextPage { get; set; }
    /// <summary>
    /// Boolean flag: true if there is previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }
    /// <summary>
    /// Total items (page size * total pages)
    /// </summary>
    public int TotalItemCount { get; set; }
    /// <summary>
    /// Total pages
    /// </summary>
    public int PageCount { get; set; }
    /// <summary>
    /// Page index (1-based index)
    /// </summary>
    public int CurrentPage { get; set; }
    /// <summary>
    /// Total items per page
    /// </summary>
    public int PageSize { get; set; }
}

/// <summary>
/// Pagination DTO, which contains the pagination metadata and the actual item list
/// </summary>
/// <param name="metadata">Pagination metadata</param>
/// <param name="items">Item list</param>
/// <typeparam name="T">Item data type</typeparam>
public class Pagination<T>(PaginationMetadata metadata, IEnumerable<T> items)
{
    /// <summary>
    /// Pagination metadata
    /// </summary>
    public PaginationMetadata Metadata { get; set; } = metadata;
    /// <summary>
    /// Item list
    /// </summary>
    public IEnumerable<T> Items { get; set; } = items;
}