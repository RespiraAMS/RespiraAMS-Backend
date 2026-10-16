using Microsoft.EntityFrameworkCore;
using Respira.Patient.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace Respira.Patient.Application.Test
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
        public async ValueTask DisposeAsync()
        {
            await _container.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Start container
            await _container.StartAsync();

            // Create DB context options
            var options = new DbContextOptionsBuilder<PatientDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            await using var context = new PatientDbContext(options);


            // Apply real migrations
            await context.Database.MigrateAsync();
        }
    }
}
