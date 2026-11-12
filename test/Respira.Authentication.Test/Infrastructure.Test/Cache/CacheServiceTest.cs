using Respira.Authentication.Infrastructure.Cache;
using ZiggyCreatures.Caching.Fusion;

namespace Respira.Authentication.Infrastructure.Test.Cache;

public sealed class CacheServiceTest : IDisposable
{
    private readonly FusionCache _fusionCache = new(new FusionCacheOptions());
    private readonly CacheService _service;

    public CacheServiceTest()
    {
        _service = new CacheService(_fusionCache);
    }

    public void Dispose()
    {
        _fusionCache.Dispose();
    }

    [Fact]
    public async Task GetAsync_MissingKey_ReturnsDefault()
    {
        var stringValue = await _service.GetAsync<string>(
            "missing-key",
            TestContext.Current.CancellationToken
        );
        var intValue = await _service.GetAsync<int>(
            "missing-int",
            TestContext.Current.CancellationToken
        );

        Assert.Null(stringValue);
        Assert.Equal(0, intValue);
    }

    [Fact]
    public async Task SetAsync_WithoutExpiration_StoresAndRetrievesValue()
    {
        await _service.SetAsync(
            "auth:otp:doctor@respira.ams",
            "123456",
            expiration: null,
            TestContext.Current.CancellationToken
        );

        var cached = await _service.GetAsync<string>(
            "auth:otp:doctor@respira.ams",
            TestContext.Current.CancellationToken
        );

        Assert.Equal("123456", cached);
    }

    [Fact]
    public async Task SetAsync_WithExplicitExpiration_StoresAndRetrievesValue()
    {
        await _service.SetAsync(
            "auth:blacklist:jti-1",
            true,
            TimeSpan.FromMinutes(5),
            TestContext.Current.CancellationToken
        );

        var cached = await _service.GetAsync<bool>(
            "auth:blacklist:jti-1",
            TestContext.Current.CancellationToken
        );

        Assert.True(cached);
    }

    [Fact]
    public async Task SetAsync_WithShortExpiration_ExpiresAfterDuration()
    {
        await _service.SetAsync(
            "auth:ephemeral",
            "temp-value",
            TimeSpan.FromMilliseconds(50),
            TestContext.Current.CancellationToken
        );

        await Task.Delay(120, TestContext.Current.CancellationToken);

        var cached = await _service.GetAsync<string>(
            "auth:ephemeral",
            TestContext.Current.CancellationToken
        );

        Assert.Null(cached);
    }

    [Fact]
    public async Task RemoveAsync_ExistingKey_EvictsEntryFromCache()
    {
        await _service.SetAsync(
            "auth:session:1",
            "active",
            TimeSpan.FromMinutes(10),
            TestContext.Current.CancellationToken
        );

        await _service.RemoveAsync("auth:session:1", TestContext.Current.CancellationToken);

        var cached = await _service.GetAsync<string>(
            "auth:session:1",
            TestContext.Current.CancellationToken
        );

        Assert.Null(cached);
    }
}
