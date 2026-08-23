using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Design;

/// <summary>
/// Design-time factory so EF Core tools can build <see cref="MediaDbContext"/> without
/// running the application host (which requires Aspire-provided connection strings).
/// </summary>
public class MediaDbContextFactory : IDesignTimeDbContextFactory<MediaDbContext>
{
    /// <summary>
    /// Builds a <see cref="MediaDbContext"/> using a local development connection string for
    /// EF Core design-time operations (migrations, scaffolding).
    /// </summary>
    /// <param name="args">Command-line arguments passed by the EF Core tooling.</param>
    /// <returns>A configured <see cref="MediaDbContext"/> instance.</returns>
    public MediaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MediaDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=respira_media;Username=postgres;Password=postgres"
        );
        return new MediaDbContext(optionsBuilder.Options);
    }
}
