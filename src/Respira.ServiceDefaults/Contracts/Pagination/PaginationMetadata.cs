namespace Respira.ServiceDefaults.Contracts.Pagination
{
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

}
