using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respira.Analysis.Infrastructure.Data;
using Respira.ServiceDefaults.Auditing;

namespace Respira.Analysis.Application.Test.Auditing;

public class AuditLoggingRegistrationTest
{
    [Fact]
    public void AddAuditLogging_PooledDbContext_CanBeResolvedFromScope()
    {
        // EF Core resolves pooled DbContext constructor dependencies from the root
        // provider, so a scoped IAuditContextAccessor makes every service fail at
        // startup with "Cannot resolve scoped service from root provider".
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuditLogging();
        services.AddDbContextPool<AnalysisDbContext>(options =>
            options.UseNpgsql("Host=localhost;Database=test;Username=test;Password=test")
        );

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true }
        );
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();

        Assert.NotNull(context);
    }

    [Fact]
    public void AddAuditLogging_RegistersAuditContextAccessor_ResolvableFromRootProvider()
    {
        using var provider = BuildProvider();

        var accessor = provider.GetRequiredService<IAuditContextAccessor>();

        Assert.NotNull(accessor);
        Assert.IsType<HttpContextAuditContextAccessor>(accessor);
    }

    [Fact]
    public void AddAuditLogging_RegistersAuditContextAccessor_AsSingleton()
    {
        using var provider = BuildProvider();

        var first = provider.GetRequiredService<IAuditContextAccessor>();
        var second = provider.GetRequiredService<IAuditContextAccessor>();

        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    [Fact]
    public void AddAuditLogging_RegistersHttpContextAccessor()
    {
        using var provider = BuildProvider();

        var accessor = provider.GetRequiredService<IHttpContextAccessor>();

        Assert.NotNull(accessor);
    }

    [Fact]
    public void AddAuditLogging_DbContextAndAccessor_AreResolvableFromScope()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<IAuditContextAccessor>();

        Assert.NotNull(context);
        Assert.NotNull(accessor);
    }

    [Fact]
    public void Accessor_WithoutHttpContext_FallsBackToSystemContext()
    {
        using var provider = BuildProvider();
        var accessor = provider.GetRequiredService<IAuditContextAccessor>();

        var current = accessor.GetCurrent();

        Assert.NotNull(current);
        Assert.Equal("System", current.ActorRole);
        Assert.Null(current.ActorAccountId);
        Assert.Null(current.RequestPath);
    }

    [Fact]
    public void AuditContextScope_Begin_OverridesAccessorResult()
    {
        using var provider = BuildProvider();
        var accessor = provider.GetRequiredService<IAuditContextAccessor>();
        var actorId = Guid.CreateVersion7();

        using var scope = AuditContextScope.Begin(
            new AuditContext(actorId, "Doctor", null, "/api/analysis", null)
        );

        var current = accessor.GetCurrent();

        Assert.NotNull(current);
        Assert.Equal(actorId, current.ActorAccountId);
        Assert.Equal("Doctor", current.ActorRole);
        Assert.Equal("/api/analysis", current.RequestPath);
    }

    [Fact]
    public void AuditContextScope_Current_IsNullWhenNoScopeIsActive()
    {
        Assert.Null(AuditContextScope.Current);
    }

    [Fact]
    public void AuditContextScope_PreferredChangeSetId_IsNullWhenNoScopeIsActive()
    {
        Assert.Null(AuditContextScope.PreferredChangeSetId);
    }

    [Fact]
    public void AuditContextScope_Begin_Dispose_RestoresThePreviousScope()
    {
        var outer = new AuditContext(Guid.CreateVersion7(), "Outer", null, null, null);
        var inner = new AuditContext(Guid.CreateVersion7(), "Inner", null, null, null);

        using (AuditContextScope.Begin(outer))
        {
            Assert.Same(outer, AuditContextScope.Current);

            using (AuditContextScope.Begin(inner))
            {
                Assert.Same(inner, AuditContextScope.Current);
            }

            Assert.Same(outer, AuditContextScope.Current);
        }

        Assert.Null(AuditContextScope.Current);
    }

    [Fact]
    public void AuditContextScope_Handle_DisposeTwice_IsSafe()
    {
        var handle = AuditContextScope.Begin(
            new AuditContext(Guid.CreateVersion7(), "Scoped", null, null, null)
        );

        Assert.NotNull(AuditContextScope.Current);

        handle.Dispose();
        handle.Dispose();

        Assert.Null(AuditContextScope.Current);
    }

    [Fact]
    public void AuditContextScope_Begin_WithPreferredChangeSetId_ExposesIt()
    {
        var changeSetId = Guid.CreateVersion7();

        using var scope = AuditContextScope.Begin(
            new AuditContext(null, "System", null, null, null),
            preferredAuditEntryId: null,
            preferredChangeSetId: changeSetId
        );

        Assert.Equal(changeSetId, AuditContextScope.PreferredChangeSetId);
    }

    [Fact]
    public void AuditContext_WithNullActorAccountId_IsAccepted()
    {
        using var provider = BuildProvider();
        var accessor = provider.GetRequiredService<IAuditContextAccessor>();
        var context = new AuditContext(null, "Anonymous", null, null, null);

        using var scope = AuditContextScope.Begin(context);

        var current = accessor.GetCurrent();

        Assert.NotNull(context);
        Assert.Null(context.ActorAccountId);
        Assert.NotNull(current);
        Assert.Null(current.ActorAccountId);
        Assert.Equal("Anonymous", current.ActorRole);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuditLogging();
        services.AddDbContextPool<AnalysisDbContext>(options =>
            options.UseNpgsql("Host=localhost;Database=test;Username=test;Password=test")
        );

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
