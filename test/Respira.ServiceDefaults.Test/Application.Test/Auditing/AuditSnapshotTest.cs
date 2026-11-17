using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Auditing;
using Respira.ServiceDefaults.Application.Test;

namespace Respira.ServiceDefaults.Application.Test.Auditing;

/// <summary>
/// Tests <see cref="AuditSnapshot.Apply"/>, which restores a stored JSON
/// snapshot onto a tracked entity so an audited change can be rolled back.
/// </summary>
public sealed class AuditSnapshotTest(PostgresFixture fixture)
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
    }

    #region Happy path

    [Fact]
    public void Apply_RestoresAStringProperty()
    {
        var target = new SnapshotTarget { Name = "Aspirin" };

        AuditSnapshot.Apply(target, """{"Name":"Paracetamol"}""", AuditSnapshot.Options);

        Assert.Equal("Paracetamol", target.Name);
    }

    [Fact]
    public void Apply_RestoresAnIntProperty()
    {
        var target = new SnapshotTarget { Quantity = 1 };

        AuditSnapshot.Apply(target, """{"Quantity":42}""", AuditSnapshot.Options);

        Assert.Equal(42, target.Quantity);
    }

    [Fact]
    public void Apply_RestoresABoolProperty()
    {
        var target = new SnapshotTarget { IsDeleted = false };

        AuditSnapshot.Apply(target, """{"IsDeleted":true}""", AuditSnapshot.Options);

        Assert.True(target.IsDeleted);
    }

    [Fact]
    public void Apply_RestoresAGuidProperty()
    {
        var id = Guid.CreateVersion7();
        var target = new SnapshotTarget();

        AuditSnapshot.Apply(target, $$"""{"Id":"{{id}}"}""", AuditSnapshot.Options);

        Assert.Equal(id, target.Id);
    }

    [Fact]
    public void Apply_RestoresADateTimeOffsetProperty()
    {
        // The shared snapshot options truncate the value to microseconds.
        var written = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero).AddTicks(1234567);
        var json = JsonSerializer.Serialize(
            new Dictionary<string, object?> { ["Timestamp"] = written },
            AuditSnapshot.Options
        );
        var target = new SnapshotTarget();

        AuditSnapshot.Apply(target, json, AuditSnapshot.Options);

        Assert.Equal(written.AddTicks(-7), target.Timestamp);
    }

    [Fact]
    public void Apply_WithoutOptions_UsesTheSharedSnapshotOptions()
    {
        var written = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero).AddTicks(1234567);
        var json = JsonSerializer.Serialize(
            new Dictionary<string, object?> { ["Timestamp"] = written },
            AuditSnapshot.Options
        );
        var target = new SnapshotTarget();

        AuditSnapshot.Apply(target, json);

        Assert.Equal(written.AddTicks(-7), target.Timestamp);
    }

    [Fact]
    public void Apply_RestoresANullablePropertyFromNull()
    {
        var target = new SnapshotTarget { Optional = "text" };

        AuditSnapshot.Apply(target, """{"Optional":null}""", AuditSnapshot.Options);

        Assert.Null(target.Optional);
    }

    [Fact]
    public void Apply_RestoresANullablePropertyFromAValue()
    {
        var target = new SnapshotTarget();

        AuditSnapshot.Apply(target, """{"Optional":"text"}""", AuditSnapshot.Options);

        Assert.Equal("text", target.Optional);
    }

    [Fact]
    public void Apply_ReturnsTheAppliedPropertyNames()
    {
        var target = new SnapshotTarget();

        var applied = AuditSnapshot.Apply(
            target,
            """{"Name":"Aspirin","Quantity":42,"IsDeleted":true}""",
            AuditSnapshot.Options
        );

        Assert.Equal(new[] { "Name", "Quantity", "IsDeleted" }, applied);
    }

    #endregion

    #region Ignored properties

    [Fact]
    public void Apply_IgnoresPropertiesThatStartWithADollarSign()
    {
        var target = new SnapshotTarget();

        var applied = AuditSnapshot.Apply(
            target,
            """{"$refs":{"Tags":["a"]},"Name":"Aspirin"}""",
            AuditSnapshot.Options
        );

        Assert.Equal("Aspirin", target.Name);
        Assert.DoesNotContain("$refs", applied);
    }

    [Fact]
    public void Apply_IgnoresPropertiesThatDoNotExistOnTheEntity()
    {
        var target = new SnapshotTarget();

        var applied = AuditSnapshot.Apply(
            target,
            """{"Unknown":"value","Name":"Aspirin"}""",
            AuditSnapshot.Options
        );

        Assert.Equal("Aspirin", target.Name);
        Assert.DoesNotContain("Unknown", applied);
    }

    [Fact]
    public void Apply_IgnoresReadOnlyProperties()
    {
        var target = new SnapshotTarget();

        var applied = AuditSnapshot.Apply(
            target,
            """{"ReadOnly":"changed"}""",
            AuditSnapshot.Options
        );

        Assert.Equal("read-only", target.ReadOnly);
        Assert.DoesNotContain("ReadOnly", applied);
    }

    [Fact]
    public void Apply_NullValueOnANonNullableValueTypeProperty_LeavesTheValueUnchanged()
    {
        var target = new SnapshotTarget { Quantity = 7 };

        var applied = AuditSnapshot.Apply(target, """{"Quantity":null}""", AuditSnapshot.Options);

        Assert.Equal(7, target.Quantity);
        Assert.DoesNotContain("Quantity", applied);
    }

    #endregion

    #region Edge cases

    [Theory]
    [InlineData("[]")]
    [InlineData("123")]
    public void Apply_ThrowsWhenTheSnapshotIsNotAJsonObject(string snapshot)
    {
        var target = new SnapshotTarget();

        Assert.Throws<InvalidOperationException>(() =>
            AuditSnapshot.Apply(target, snapshot, AuditSnapshot.Options)
        );
    }

    [Fact]
    public void Apply_ThrowsForANullEntity()
    {
        Assert.Throws<ArgumentNullException>(() =>
            AuditSnapshot.Apply(null!, """{"Name":"Aspirin"}""", AuditSnapshot.Options)
        );
    }

    [Fact]
    public void Apply_WithMutateInsteadOfReplace_MutatesTheExistingInstance()
    {
        var details = new DetailsPayload { Label = "old", Level = 1 };
        var target = new SnapshotTarget { Details = details };

        var applied = AuditSnapshot.Apply(
            target,
            """{"Details":{"Label":"new","Level":2}}""",
            AuditSnapshot.Options,
            new HashSet<string> { "Details" }
        );

        Assert.Same(details, target.Details);
        Assert.Equal("new", details.Label);
        Assert.Equal(2, details.Level);
        Assert.Contains("Details", applied);
    }

    [Fact]
    public async Task Apply_RestoresAnEntityFromTheDatabaseSnapshot()
    {
        await using var context = new PostgresFixture.AuditTestDbContext(_options);
        var entity = new PostgresFixture.AuditTestEntity { Name = "Aspirin", Quantity = 10 };
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        var tracked = await context.Entities.SingleAsync(
            x => x.Id == entity.Id,
            TestContext.Current.CancellationToken
        );
        var entry = context.ChangeTracker.Entries<PostgresFixture.AuditTestEntity>().Single();
        var snapshot = AuditSnapshot.ToJson(entry);

        tracked.Name = "Changed";
        tracked.Quantity = 99;

        var restored = new PostgresFixture.AuditTestEntity
        {
            Id = entity.Id,
            Name = "unknown",
            Quantity = 0,
        };
        var applied = AuditSnapshot.Apply(restored, snapshot, AuditSnapshot.Options);

        Assert.Equal("Aspirin", restored.Name);
        Assert.Equal(10, restored.Quantity);
        Assert.Contains("Name", applied);
    }

    #endregion

    private sealed class SnapshotTarget
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string? Optional { get; set; }
        public DetailsPayload? Details { get; set; }
        public string ReadOnly => "read-only";
    }

    private sealed class DetailsPayload
    {
        public string Label { get; set; } = string.Empty;
        public int Level { get; set; }
    }
}
