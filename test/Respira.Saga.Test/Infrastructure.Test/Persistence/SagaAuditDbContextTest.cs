using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respira.Saga.Domain.Entities;
using Respira.Saga.Infrastructure.Persistence;

namespace Respira.Saga.Infrastructure.Test.Persistence;

public sealed class SagaAuditDbContextTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<SagaAuditDbContext> _options;
    private readonly SagaAuditDbContext _context;

    public SagaAuditDbContextTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<SagaAuditDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        _context = new SagaAuditDbContext(_options);
    }

    public async ValueTask InitializeAsync()
    {
        await _context.AuditLogs.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public void Model_ConfiguresAuditLogEntryTableColumnsAndIndexes()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLogEntry));

        Assert.NotNull(entityType);
        Assert.Equal("AuditLogs", entityType!.GetTableName());

        var idProperty = entityType.FindProperty(nameof(AuditLogEntry.Id))!;
        Assert.Equal(
            Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never,
            idProperty.ValueGenerated
        );

        var actionProperty = entityType.FindProperty(nameof(AuditLogEntry.Action))!;
        Assert.Equal(128, actionProperty.GetMaxLength());
        Assert.False(actionProperty.IsNullable);

        var actorRoleProperty = entityType.FindProperty(nameof(AuditLogEntry.ActorRole))!;
        Assert.Equal(64, actorRoleProperty.GetMaxLength());
        Assert.False(actorRoleProperty.IsNullable);

        var entityTypeProperty = entityType.FindProperty(nameof(AuditLogEntry.EntityType))!;
        Assert.Equal(128, entityTypeProperty.GetMaxLength());
        Assert.False(entityTypeProperty.IsNullable);

        var detailsProperty = entityType.FindProperty(nameof(AuditLogEntry.DetailsJson))!;
        Assert.Equal("jsonb", detailsProperty.GetColumnType());
        Assert.False(detailsProperty.IsNullable);

        var indexes = entityType.GetIndexes().ToList();
        Assert.Contains(
            indexes,
            idx =>
                idx.Properties.Select(p => p.Name).SequenceEqual([nameof(AuditLogEntry.OccurredAt)])
        );
        Assert.Contains(
            indexes,
            idx =>
                idx.Properties.Select(p => p.Name)
                    .SequenceEqual([nameof(AuditLogEntry.EntityType), nameof(AuditLogEntry.EntityId)])
        );
        Assert.Contains(
            indexes,
            idx =>
                idx.Properties.Select(p => p.Name)
                    .SequenceEqual([nameof(AuditLogEntry.ActorAccountId)])
        );
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsAndReadsBackAuditLogEntryWithJsonbDetails()
    {
        var entry = new AuditLogEntry
        {
            Id = Guid.CreateVersion7(),
            OccurredAt = new DateTimeOffset(2026, 11, 12, 9, 0, 0, TimeSpan.Zero),
            ActorAccountId = Guid.CreateVersion7(),
            ActorRole = "Admin",
            Action = "Saga.Profile.Created",
            EntityType = "Doctor",
            EntityId = Guid.CreateVersion7(),
            CorrelationId = Guid.CreateVersion7(),
            DetailsJson = """{"step":"Completed","email":"doctor@respira.ams"}""",
        };

        _context.AuditLogs.Add(entry);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verifyContext = new SagaAuditDbContext(_options);
        var persisted = await verifyContext
            .AuditLogs.AsNoTracking()
            .SingleAsync(x => x.Id == entry.Id, TestContext.Current.CancellationToken);

        Assert.Equal(entry.Id, persisted.Id);
        Assert.Equal(entry.OccurredAt, persisted.OccurredAt);
        Assert.Equal(entry.ActorAccountId, persisted.ActorAccountId);
        Assert.Equal("Admin", persisted.ActorRole);
        Assert.Equal("Saga.Profile.Created", persisted.Action);
        Assert.Equal("Doctor", persisted.EntityType);
        Assert.Equal(entry.EntityId, persisted.EntityId);
        Assert.Equal(entry.CorrelationId, persisted.CorrelationId);

        using var doc = JsonDocument.Parse(persisted.DetailsJson);
        Assert.Equal("Completed", doc.RootElement.GetProperty("step").GetString());
        Assert.Equal("doctor@respira.ams", doc.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task SaveChangesAsync_DuplicateId_ThrowsUniqueViolation()
    {
        var id = Guid.CreateVersion7();
        _context.AuditLogs.Add(
            new AuditLogEntry
            {
                Id = id,
                OccurredAt = DateTimeOffset.UtcNow,
                ActorRole = "Doctor",
                Action = "Created",
                EntityType = "Patient",
                DetailsJson = "{}",
            }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var secondContext = new SagaAuditDbContext(_options);
        secondContext.AuditLogs.Add(
            new AuditLogEntry
            {
                Id = id,
                OccurredAt = DateTimeOffset.UtcNow,
                ActorRole = "Doctor",
                Action = "Updated",
                EntityType = "Patient",
                DetailsJson = "{}",
            }
        );

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() =>
            secondContext.SaveChangesAsync(TestContext.Current.CancellationToken)
        );
        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, pg.SqlState);
    }
}
