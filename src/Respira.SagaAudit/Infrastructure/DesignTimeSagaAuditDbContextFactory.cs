using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Infrastructure.Persistence.Database;

namespace Respira.SagaAudit.Infrastructure;

/// <summary>
/// Design-time factory for EF Core migrations.
/// </summary>
public class DesignTimeSagaAuditDbContextFactory : IDesignTimeDbContextFactory<SagaAuditDbContext>
{
    /// <summary>
    /// Builds a <see cref="SagaAuditDbContext"/> for EF Core design-time tooling
    /// (migrations) using a local PostgreSQL connection string.
    /// </summary>
    /// <param name="args">Command-line arguments passed by the EF tooling.</param>
    /// <returns>A configured <see cref="SagaAuditDbContext"/> instance.</returns>
    public SagaAuditDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SagaAuditDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=respira_saga_audit;Username=postgres;Password=postgres");
        return new SagaAuditDbContext(optionsBuilder.Options);
    }
}
