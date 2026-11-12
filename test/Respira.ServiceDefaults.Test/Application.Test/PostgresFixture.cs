using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Auditing;
using Testcontainers.PostgreSql;

namespace Respira.ServiceDefaults.Application.Test;

/// <summary>
/// Postgres container used by the audit infrastructure tests.
/// The <c>audit_entries</c> table is created by the migration of the audited service,
/// so the fixture only provides an empty database and the test DbContext applies
/// the schema itself through <c>EnsureCreated</c>.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.1-alpine3.22")
        .WithDatabase("audit_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    public DbContextOptions<AuditTestDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<AuditTestDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

    /// <summary>
    /// A minimal audited context, mirroring how the real services derive from
    /// <see cref="AuditableDbContext"/>.
    /// </summary>
    public sealed class AuditTestDbContext(
        DbContextOptions<AuditTestDbContext> options,
        IAuditContextAccessor? accessor = null
    ) : AuditableDbContext(options, "Test", accessor)
    {
        public DbSet<AuditTestEntity> Entities => Set<AuditTestEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ConfigureAuditEntries();
            modelBuilder.Entity<AuditTestEntity>(entity =>
            {
                entity.ToTable("audit_test_entities");
                entity.HasKey(item => item.Id);
            });
        }
    }

    public sealed class AuditTestEntity
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public bool IsDeleted { get; set; }
    }
}
