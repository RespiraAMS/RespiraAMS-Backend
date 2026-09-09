namespace Respira.ServiceDefaults.Contracts.Pagination
{
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


}
