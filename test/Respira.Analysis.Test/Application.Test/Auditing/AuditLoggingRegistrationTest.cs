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
}
