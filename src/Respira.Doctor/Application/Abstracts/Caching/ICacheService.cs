namespace Application.Abstracts.Caching
{
    /// <summary>
    /// Abstraction over the distributed cache (Redis via FusionCache).
    /// Used to speed up doctor profile lookups.
    /// </summary>
    public interface ICacheService
    {
        public Task<T> GetAsync<T>(string key);
        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        public Task RemoveAsync(string key);
    }
}
