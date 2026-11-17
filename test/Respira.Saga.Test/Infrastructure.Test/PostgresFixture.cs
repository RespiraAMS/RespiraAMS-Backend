using Microsoft.EntityFrameworkCore;
using Respira.Saga.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Respira.Saga.Infrastructure.Test;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.1-alpine3.22")
        .WithDatabase("saga_infra_test")
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

    public SagaAuditDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SagaAuditDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new SagaAuditDbContext(options);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
