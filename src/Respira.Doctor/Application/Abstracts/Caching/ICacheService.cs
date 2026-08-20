namespace Application.Abstracts.Caching
{
    /// <summary>
    /// Cache service interface
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Get an item from cache (Redis)
        /// </summary>
        /// <typeparam name="T">T is the type of the object</typeparam>
        /// <param name="key">the key</param>
        /// <returns>Task of the object</returns>
        public Task<T> GetAsync<T>(string key);

        /// <summary>
        /// Set an item in cache (Redis)
        /// </summary>
        /// <typeparam name="T">T is the type of the object</typeparam>
        /// <param name="key">the key</param>
        /// <param name="value">the value</param>
        /// <param name="expiration">the expiration</param>
        /// <returns>Task</returns>
        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// Remove an item from cache (Redis)
        /// </summary>
        /// <param name="key">the key</param>
        /// <returns>Task</returns>
        public Task RemoveAsync(string key);
    }
}
