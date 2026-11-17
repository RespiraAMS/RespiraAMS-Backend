using Microsoft.EntityFrameworkCore;
using Respira.Doctor.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Respira.Doctor.Application.Test;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.1-alpine3.22")
        .WithDatabase("doctor_test")
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

    public DoctorDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DoctorDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new DoctorDbContext(options);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
