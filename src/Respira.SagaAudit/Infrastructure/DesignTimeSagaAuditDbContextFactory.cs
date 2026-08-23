using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Respira.SagaAudit.Infrastructure.Persistence.Database;

namespace Respira.SagaAudit.Infrastructure;

/// <summary>
/// Design-time factory for EF Core migrations.
/// </summary>
public class DesignTimeSagaAuditDbContextFactory : IDesignTimeDbContextFactory<SagaAuditDbContext>
{
    public SagaAuditDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SagaAuditDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=respira_saga_audit;Username=postgres;Password=postgres");
        return new SagaAuditDbContext(optionsBuilder.Options);
    }
}
