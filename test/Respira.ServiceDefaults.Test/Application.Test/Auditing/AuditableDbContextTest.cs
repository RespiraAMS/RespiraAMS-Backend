using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Auditing;
using Respira.ServiceDefaults.Application.Test;

namespace Respira.ServiceDefaults.Application.Test.Auditing;

/// <summary>
/// Tests the automatic audit capture of <see cref="AuditableDbContext"/>: every
/// entity change must produce one durable row in the same transaction as the
/// business change.
/// </summary>
public sealed class AuditableDbContextTest(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>,
        IAsyncLifetime
{
    private readonly DbContextOptions<PostgresFixture.AuditTestDbContext> _options =
        fixture.CreateOptions();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public async ValueTask InitializeAsync()
    {
        await using var context = new PostgresFixture.AuditTestDbContext(_options);
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await context
            .AuditEntries.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context
            .Entities.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    #region Happy path

    [Fact]
    public async Task SaveChangesAsync_Insert_WritesCreatedAuditEntry()
    {
        var entity = new PostgresFixture.AuditTestEntity { Name = "Aspirin", Quantity = 10 };

        await using (var context = new PostgresFixture.AuditTestDbContext(_options))
        {
            context.Entities.Add(entity);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verification = new PostgresFixture.AuditTestDbContext(_options);
        var entry = await verification.AuditEntries.SingleAsync(
            TestContext.Current.CancellationToken
        );

        Assert.Equal("Test.AuditTestEntity.Created", entry.Action);
        Assert.Equal("Created", entry.Operation);
        Assert.Equal(
            "Respira.ServiceDefaults.Application.Test.PostgresFixture+AuditTestEntity",
            entry.EntityType
        );
        Assert.Equal(entity.Id, entry.EntityId);
        Assert.NotEqual(Guid.Empty, entry.ChangeSetId);
        Assert.Null(entry.BeforeJson);
        Assert.NotNull(entry.AfterJson);
    }

    [Fact]
    public async Task SaveChangesAsync_Update_WritesUpdatedAuditEntry()
    {
        var entity = await SeedAsync();

        await using (var context = new PostgresFixture.AuditTestDbContext(_options))
        {
            var tracked = await context.Entities.SingleAsync(
                x => x.Id == entity.Id,
                TestContext.Current.CancellationToken
            );
            tracked.Name = "Paracetamol";
            tracked.Quantity = 20;
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verification = new PostgresFixture.AuditTestDbContext(_options);
        var entry = await verification.AuditEntries.SingleAsync(
            x => x.Operation == "Updated",
            TestContext.Current.CancellationToken
        );

        Assert.Equal("Test.AuditTestEntity.Updated", entry.Action);
        Assert.Contains("Name", entry.ChangedFieldsJson);
        Assert.Contains("Quantity", entry.ChangedFieldsJson);
        Assert.NotNull(entry.BeforeJson);
        Assert.NotNull(entry.AfterJson);
    }

    [Fact]
    public async Task SaveChangesAsync_Update_CapturesBeforeAndAfterValues()
    {
        var entity = await SeedAsync();

        await using (var context = new PostgresFixture.AuditTestDbContext(_options))
        {
            var tracked = await context.Entities.SingleAsync(
                x => x.Id == entity.Id,
                TestContext.Current.CancellationToken
            );
            tracked.Name = "Changed";
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verification = new PostgresFixture.AuditTestDbContext(_options);
        var entry = await verification.AuditEntries.SingleAsync(
            x => x.Operation == "Updated",
            TestContext.Current.CancellationToken
        );

        using var before = JsonDocument.Parse(entry.BeforeJson!);
        using var after = JsonDocument.Parse(entry.AfterJson!);

        Assert.Equal("Aspirin", before.RootElement.GetProperty("Name").GetString());
        Assert.Equal("Changed", after.RootElement.GetProperty("Name").GetString());
    }

    [Fact]
    public async Task SaveChangesAsync_SoftDelete_WritesSoftDeletedAuditEntry()
    {
        var entity = await SeedAsync();

        await using (var context = new PostgresFixture.AuditTestDbContext(_options))
        {
            var tracked = await context.Entities.SingleAsync(
                x => x.Id == entity.Id,
                TestContext.Current.CancellationToken
            );
            tracked.IsDeleted = true;
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verification = new PostgresFixture.AuditTestDbContext(_options);
        var entry = await verification.AuditEntries.SingleAsync(
            x => x.Operation == "SoftDeleted",
            TestContext.Current.CancellationToken
        );
        Assert.Equal("SoftDeleted", entry.Operation);
    }

    [Fact]
    public async Task SaveChangesAsync_Restore_WritesRestoredAuditEntry()
    {
        var entity = await SeedAsync();

        await using (var context = new PostgresFixture.AuditTestDbContext(_options))
        {
            var tracked = await context.Entities.SingleAsync(
                x => x.Id == entity.Id,
                TestContext.Current.CancellationToken
            );
            tracked.IsDeleted = true;
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = new PostgresFixture.AuditTestDbContext(_options))
        {
            var tracked = await context.Entities.SingleAsync(
                x => x.Id == entity.Id,
                TestContext.Current.CancellationToken
            );
            tracked.IsDeleted = false;
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verification = new PostgresFixture.AuditTestDbContext(_options);
        var entry = await verification.AuditEntries.SingleAsync(
            x => x.Operation == "Restored",
            TestContext.Current.CancellationToken
        );
        Assert.Equal("Restored", entry.Operation);
    }

    [Fact]
    public async Task SaveChangesAsync_HardDelete_WritesDeletedAuditEntry()
    {
        var entity = await SeedAsync();

        await using (var context = new PostgresFixture.AuditTestDbContext(_options))
        {
            var tracked = await context.Entities.SingleAsync(
                x => x.Id == entity.Id,
                TestContext.Current.CancellationToken
            );
            context.Entities.Remove(tracked);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verification = new PostgresFixture.AuditTestDbContext(_options);
        var entry = await verification.AuditEntries.SingleAsync(
            x => x.Operation == "Deleted",
            TestContext.Current.CancellationToken
        );
        Assert.Equal("Deleted", entry.Operation);
        Assert.NotNull(entry.BeforeJson);
        Assert.Null(entry.AfterJson);
    }

    [Fact]
    public async Task SaveChangesAsync_MultipleChanges_ShareOneChangeSetId()
    {
        await using (var context = new PostgresFixture.AuditTestDbContext(_options))
        {
            context.Entities.Add(new PostgresFixture.AuditTestEntity { Name = "A", Quantity = 1 });
            context.Entities.Add(new PostgresFixture.AuditTestEntity { Name = "B", Quantity = 2 });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verification = new PostgresFixture.AuditTestDbContext(_options);
        var entries = await verification.AuditEntries.ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, entries.Count);
        Assert.Single(entries.Select(x => x.ChangeSetId).Distinct());
    }

    [Fact]
    public async Task SaveChangesAsync_SeparateCalls_UseDifferentChangeSetIds()
    {
        await using (var context = new PostgresFixture.AuditTestDbContext(_options))
        {
            context.Entities.Add(new PostgresFixture.AuditTestEntity { Name = "A" });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            context.Entities.Add(new PostgresFixture.AuditTestEntity { Name = "B" });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verification = new PostgresFixture.AuditTestDbContext(_options);
        var entries = await verification.AuditEntries.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, entries.Select(x => x.ChangeSetId).Distinct().Count());
    }

    [Fact]
    public async Task SaveChangesAsync_NoChanges_WritesNoAuditEntry()
    {
        var entity = await SeedAsync();

        // The seed itself wrote a "Created" entry; drop it so the assertion only
        // covers what the no-op save below produces.
        await using (var context = new PostgresFixture.AuditTestDbContext(_options))
        {
            await context.AuditEntries.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = new PostgresFixture.AuditTestDbContext(_options))
        {
            var tracked = await context.Entities.SingleAsync(
                x => x.Id == entity.Id,
                TestContext.Current.CancellationToken
            );
            tracked.Name = tracked.Name;
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verification = new PostgresFixture.AuditTestDbContext(_options);
        Assert.Empty(await verification.AuditEntries.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveChangesAsync_UsesTheScopedAuditContext()
    {
        var actorId = Guid.CreateVersion7();
        var correlationId = Guid.CreateVersion7();

        using (AuditContextScope.Begin(
            new AuditContext(actorId, "Admin", correlationId, "/api/v1/things", "trace-1")
        ))
        {
            await using var context = new PostgresFixture.AuditTestDbContext(_options);
            context.Entities.Add(new PostgresFixture.AuditTestEntity { Name = "Scoped" });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verification = new PostgresFixture.AuditTestDbContext(_options);
        var entry = await verification.AuditEntries.SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(actorId, entry.ActorAccountId);
        Assert.Equal("Admin", entry.ActorRole);
        Assert.Equal(correlationId, entry.CorrelationId);
        Assert.Equal("/api/v1/things", entry.RequestPath);
        Assert.Equal("trace-1", entry.TraceId);
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutScopedContext_UsesSystemActor()
    {
        await using var context = new PostgresFixture.AuditTestDbContext(_options);
        context.Entities.Add(new PostgresFixture.AuditTestEntity { Name = "System" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verification = new PostgresFixture.AuditTestDbContext(_options);
        var entry = await verification.AuditEntries.SingleAsync(TestContext.Current.CancellationToken);

        Assert.Null(entry.ActorAccountId);
        Assert.Equal("System", entry.ActorRole);
        Assert.Null(entry.CorrelationId);
    }

    [Fact]
    public async Task SaveChangesAsync_HonoursThePreferredChangeSetId()
    {
        var changeSetId = Guid.CreateVersion7();

        using (AuditContextScope.Begin(AuditContext.System, preferredChangeSetId: changeSetId))
        {
            await using var context = new PostgresFixture.AuditTestDbContext(_options);
            context.Entities.Add(new PostgresFixture.AuditTestEntity { Name = "Preferred" });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verification = new PostgresFixture.AuditTestDbContext(_options);
        var entry = await verification.AuditEntries.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(changeSetId, entry.ChangeSetId);
    }

    [Fact]
    public async Task SaveChangesAsync_HonoursThePreferredAuditEntryId()
    {
        var entryId = Guid.CreateVersion7();

        using (AuditContextScope.Begin(AuditContext.System, preferredAuditEntryId: entryId))
        {
            await using var context = new PostgresFixture.AuditTestDbContext(_options);
            context.Entities.Add(new PostgresFixture.AuditTestEntity { Name = "Preferred" });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verification = new PostgresFixture.AuditTestDbContext(_options);
        var entry = await verification.AuditEntries.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(entryId, entry.Id);
    }

    [Fact]
    public async Task SuppressAuditCapture_SkipsTheAutomaticEntries()
    {
        using (AuditContextScope.Begin(AuditContext.System))
        {
            await using var context = new PostgresFixture.AuditTestDbContext(_options);
            context.Entities.Add(new PostgresFixture.AuditTestEntity { Name = "Suppressed" });

            using (context.SuppressAuditCapture())
            {
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }
        }

        await using var verification = new PostgresFixture.AuditTestDbContext(_options);
        Assert.Empty(await verification.AuditEntries.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SuppressAuditCapture_NestedScopes_AreRestored()
    {
        await using var context = new PostgresFixture.AuditTestDbContext(_options);
        context.Entities.Add(new PostgresFixture.AuditTestEntity { Name = "Nested" });

        using (context.SuppressAuditCapture())
        using (context.SuppressAuditCapture())
        {
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verification = new PostgresFixture.AuditTestDbContext(_options);
        Assert.Empty(await verification.AuditEntries.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SuppressAuditCapture_AfterDisposing_WritesEntriesAgain()
    {
        await using (var context = new PostgresFixture.AuditTestDbContext(_options))
        {
            context.Entities.Add(new PostgresFixture.AuditTestEntity { Name = "First" });
            using (context.SuppressAuditCapture())
            {
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }
        }

        await using (var context = new PostgresFixture.AuditTestDbContext(_options))
        {
            context.Entities.Add(new PostgresFixture.AuditTestEntity { Name = "Second" });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verification = new PostgresFixture.AuditTestDbContext(_options);
        var entries = await verification.AuditEntries.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(entries);
    }

    [Fact]
    public async Task SaveChangesAsync_AuditRowIsNotAuditedItself()
    {
        await using var context = new PostgresFixture.AuditTestDbContext(_options);
        context.Entities.Add(new PostgresFixture.AuditTestEntity { Name = "Self" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verification = new PostgresFixture.AuditTestDbContext(_options);
        var entries = await verification.AuditEntries.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(entries);
        Assert.DoesNotContain(entries, x => x.EntityType == "AuditEntry");
    }

    #endregion

    private async Task<PostgresFixture.AuditTestEntity> SeedAsync()
    {
        await using var context = new PostgresFixture.AuditTestDbContext(_options);
        var entity = new PostgresFixture.AuditTestEntity
        {
            Name = "Aspirin",
            Quantity = 10,
        };
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return entity;
    }
}
