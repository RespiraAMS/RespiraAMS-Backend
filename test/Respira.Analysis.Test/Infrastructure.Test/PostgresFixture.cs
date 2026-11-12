using Microsoft.EntityFrameworkCore;
using Respira.Analysis.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace Respira.Analysis.Infrastructure.Test;

/// <summary>
/// Spins up a throwaway PostgreSQL container and applies the real Analysis
/// migrations so the infrastructure tests run against the production schema.
/// </summary>
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public string ConnectionString => _container.GetConnectionString();

    public PostgresFixture()
    {
        _container = new PostgreSqlBuilder("postgres:18.1-alpine3.22")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<AnalysisDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var context = new AnalysisDbContext(options);
        await context.Database.MigrateAsync();
    }
}
