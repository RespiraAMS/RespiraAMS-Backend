using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace Respira.Application.Test
{
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
            var options = new DbContextOptionsBuilder<ClinicalDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            await using var context = new ClinicalDbContext(options);

            // Apply real migrations
            await context.Database.MigrateAsync();
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            await _container.DisposeAsync();
        }
    }
}
