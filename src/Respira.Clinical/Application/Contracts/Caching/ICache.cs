namespace Respira.Clinical.Application.Contracts.Caching
{
    public interface ICache
    {
        /// <summary>
        /// Get a cached object using a key
        /// </summary>
        /// <typeparam name="T">The type of object that will be fetched from the cache</typeparam>
        /// <param name="key">Object key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The object (null if not found) associate with the key.</returns>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a cached object using a key
        /// </summary>
        /// <typeparam name="T">The type of object that will be fetched from the cache</typeparam>
        /// <param name="id">Object UUID key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The object (null if not found) associate with the key.</returns>
        Task<T?> GetAsync<T>(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Set an object into the cache with a key
        /// </summary>
        /// <typeparam name="T">The type of object that will be fetched from the cache</typeparam>
        /// <param name="key">Object key</param>
        /// <param name="value">Actual value to be written</param>
        /// <param name="expiration">Expiration time</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task object when success</returns>
        Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Set an object into the cache with a key
        /// </summary>
        /// <typeparam name="T">The type of object that will be fetched from the cache</typeparam>
        /// <param name="id">Object UUID key</param>
        /// <param name="value">Actual value to be written</param>
        /// <param name="expiration">Expiration time</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task object when success</returns>
        Task SetAsync<T>(Guid id, T value, TimeSpan expiration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove an entry out of cache using a key
        /// </summary>
        /// <param name="key">Object key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task object when succes</returns>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove an entry out of cache using a key
        /// </summary>
        /// <param name="id">Object UUID key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task object when succes</returns>
        Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
