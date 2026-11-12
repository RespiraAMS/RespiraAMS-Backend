using Microsoft.EntityFrameworkCore;
using Respira.Authentication.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Respira.Authentication.Infrastructure.Test;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.1-alpine3.22")
        .WithDatabase("auth_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateContext();
        await dbContext.Database.MigrateAsync();
    }

    public AuthDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new AuthDbContext(options);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
