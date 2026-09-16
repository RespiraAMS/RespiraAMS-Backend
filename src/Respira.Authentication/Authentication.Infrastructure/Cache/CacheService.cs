using Authentication.Application.Constracts.Cache;
using ZiggyCreatures.Caching.Fusion;

namespace Authentication.Infrastructure.Cache
{
    public class CacheService(IFusionCache cache) : ICacheService
    {
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return await cache.GetOrDefaultAsync<T>(key, default, token: cancellationToken);
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            await cache.RemoveAsync(key, token: cancellationToken);
        }

        public async Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default
        )
        {
            if (expiration is null)
            {
                await cache.SetAsync(key, value, token: cancellationToken);
            }
            else
            {
                await cache.SetAsync(key, value, expiration.Value, token: cancellationToken);
            }
        }
    }
}
