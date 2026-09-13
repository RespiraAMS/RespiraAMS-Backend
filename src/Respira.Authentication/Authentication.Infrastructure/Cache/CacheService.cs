using Authentication.Application.Constracts.Cache;
using ZiggyCreatures.Caching.Fusion;

namespace Authentication.Infrastructure.Cache
{
    public class CacheService(IFusionCache cache) : ICacheService
    {
        public async Task<T?> GetAsync<T>(string key)
        {
            return await cache.GetOrDefaultAsync<T>(key, default);
        }

        public async Task RemoveAsync(string key)
        {
            await cache.RemoveAsync(key);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (expiration is null)
            {
                await cache.SetAsync(key, value);
            }
            else
            {
                await cache.SetAsync(key, value, expiration.Value);
            }
        }
    }
}
