using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure;

/// <summary>
/// Design-time factory used by the EF Core tooling to build the AuthDbContext
/// when generating migrations. The connection string here is only used at design
/// time to build the model; at runtime the context is configured via Aspire.
/// </summary>
public class DesignTimeAuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    /// <summary>
    /// Creates an AuthDbContext instance for the EF Core tooling
    /// </summary>
    /// <param name="args">Command-line arguments from the EF Core tools</param>
    /// <returns>A configured AuthDbContext</returns>
    public AuthDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=auth_db;Username=postgres;Password=postgres"
        );
        return new AuthDbContext(optionsBuilder.Options);
    }
}
