using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Respira.Authentication.Application.Contracts.Authentication;
using Respira.Authentication.Application.Contracts.Cache;
using Respira.Authentication.Application.Contracts.Data;
using Respira.Authentication.Application.Contracts.Email;
using Respira.Authentication.Infrastructure.Authentication;
using Respira.Authentication.Infrastructure.Cache;
using Respira.Authentication.Infrastructure.Email;
using Respira.Authentication.Infrastructure.Extensions;
using ZiggyCreatures.Caching.Fusion;

namespace Respira.Authentication.Infrastructure.Test.Extensions;

public class DependencyInjectionTest
{
    [Fact]
    public void AddAuthenticationInfrastructure_RegistersExpectedServiceDescriptorsAndLifetimes()
    {
        var services = new ServiceCollection();

        var returned = services.AddAuthenticationInfrastructure();

        Assert.Same(services, returned);

        var emailDescriptor = Assert.Single(services, d => d.ServiceType == typeof(IEmailService));
        Assert.Equal(typeof(EmailService), emailDescriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, emailDescriptor.Lifetime);

        var hashDescriptor = Assert.Single(services, d => d.ServiceType == typeof(IHashService));
        Assert.Equal(typeof(HashService), hashDescriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, hashDescriptor.Lifetime);

        var jwtDescriptor = Assert.Single(services, d => d.ServiceType == typeof(IJwtService));
        Assert.Equal(typeof(JwtService), jwtDescriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, jwtDescriptor.Lifetime);

        var cacheDescriptor = Assert.Single(services, d => d.ServiceType == typeof(ICacheService));
        Assert.Equal(typeof(CacheService), cacheDescriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, cacheDescriptor.Lifetime);
    }

    [Fact]
    public void AddAuthenticationInfrastructure_ResolvesAllServicesWithValidateScopesEnabled()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IFusionCache>(_ => new FusionCache(new FusionCacheOptions()));
        services.AddScoped(_ => Mock.Of<IAuthDbContext>());
        services.AddSingleton(
            Options.Create(
                new EmailOption
                {
                    Host = "localhost",
                    Port = 587,
                    UserName = "user",
                    Password = "password",
                    FromEmail = "noreply@respira.ams",
                    FromName = "Respira",
                }
            )
        );
        services.AddSingleton(
            Options.Create(
                new JwtOption
                {
                    Issuer = "issuer",
                    Audience = "audience",
                    Secret = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                    AccessTokenExpires = 15,
                    RefreshTokenExpires = 7,
                }
            )
        );

        services.AddAuthenticationInfrastructure();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true }
        );
        using var scope = provider.CreateScope();

        Assert.IsType<EmailService>(scope.ServiceProvider.GetRequiredService<IEmailService>());
        Assert.IsType<HashService>(scope.ServiceProvider.GetRequiredService<IHashService>());
        Assert.IsType<JwtService>(scope.ServiceProvider.GetRequiredService<IJwtService>());
        Assert.IsType<CacheService>(scope.ServiceProvider.GetRequiredService<ICacheService>());
    }
}
