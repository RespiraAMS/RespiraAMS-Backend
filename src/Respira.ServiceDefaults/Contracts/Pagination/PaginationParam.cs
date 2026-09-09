namespace Respira.ServiceDefaults.Contracts.Pagination
{
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

}
