using Application.Abstracts.Caching;
using ZiggyCreatures.Caching.Fusion;

namespace Infrastructure.Caching
{
    /// <summary>
    /// FusionCache-backed implementation of <see cref="ICacheService"/>.
    /// </summary>
    /// <param name="cache">FusionCache instance</param>
    public class CacheService(IFusionCache cache) : ICacheService
    {
        private readonly IFusionCache _cache = cache;

        /// <summary>Reads a value from the cache; returns default if missing</summary>
        /// <typeparam name="T">Type of the cached value</typeparam>
        /// <param name="key">Cache key</param>
        /// <returns>The cached value or default</returns>
        public async Task<T> GetAsync<T>(string key)
        {
            var value = await _cache.TryGetAsync<T>(key);
            return value.HasValue ? value.Value : default!;
        }

        /// <summary>Removes a value from the cache</summary>
        /// <param name="key">Cache key</param>
        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }

        /// <summary>Stores a value in the cache, optionally with a TTL</summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="value">Value to store</param>
        /// <param name="expiration">Optional expiration window</param>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (expiration is null)
            {
                await _cache.SetAsync(key, value);
            }
            else
            {
                await _cache.SetAsync(key, value, expiration.Value);
            }
        }
    }
}
