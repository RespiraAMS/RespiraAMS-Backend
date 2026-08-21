using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
namespace Application.Test;

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
    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        // Start container
        await _container.StartAsync();

        // Create DB context options
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var context = new AppDbContext(options);

        // Apply real migrations
        await context.Database.MigrateAsync();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
