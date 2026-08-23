namespace Application.Abstracts.Caching
{
    /// <summary>
    /// Abstraction over the distributed cache (Redis via FusionCache).
    /// Used to speed up doctor profile lookups.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>Reads a cached value by key</summary>
        /// <typeparam name="T">Type of the cached value</typeparam>
        /// <param name="key">Cache key</param>
        /// <returns>The cached value, or default if not present</returns>
        public Task<T> GetAsync<T>(string key);

        /// <summary>Stores a value in the cache</summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="value">Value to store</param>
        /// <param name="expiration">Optional expiration window</param>
        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>Removes a value from the cache</summary>
        /// <param name="key">Cache key</param>
        public Task RemoveAsync(string key);
    }
}
