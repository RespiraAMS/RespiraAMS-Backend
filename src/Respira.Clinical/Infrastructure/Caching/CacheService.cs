using Respira.Clinical.Application.Contracts.Caching;
using ZiggyCreatures.Caching.Fusion;

namespace Respira.Clinical.Infrastructure.Caching
{
    public class CacheService(IFusionCache cache) : ICache
    {
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return await cache.GetOrDefaultAsync<T>(key, default, token: cancellationToken);
        }

        public async Task<T?> GetAsync<T>(Guid id, CancellationToken cancellationToken = default)
        {
            return await GetAsync<T>(id.ToString(), cancellationToken);
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            await cache.RemoveAsync(key, token: cancellationToken);
        }

        public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await RemoveAsync(id.ToString(), cancellationToken);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
        {
            await cache.SetAsync(key, value, expiration, token: cancellationToken);
        }

        public async Task SetAsync<T>(Guid id, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
        {
            await SetAsync(id.ToString(), value, expiration, cancellationToken);
        }
    }
}
