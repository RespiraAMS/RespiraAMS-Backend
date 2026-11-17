using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Auditing;
using Respira.ServiceDefaults.Application.Test;
using Respira.ServiceDefaults.Contracts.Messages.Audit;

namespace Respira.ServiceDefaults.Application.Test.Auditing;

/// <summary>
/// Tests <see cref="AuditOutboxForwarder{TDbContext}"/>, the background service
/// that publishes the local audit outbox to the central Saga audit store and
/// marks the forwarded rows so they are not published twice.
/// </summary>
public sealed class AuditOutboxForwarderTest(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>,
        IAsyncLifetime
{
    /// <summary>The forwarder polls every two seconds, so one tick needs a bit more.</summary>
    private static readonly TimeSpan OneTick = TimeSpan.FromSeconds(3);

    private readonly DbContextOptions<PostgresFixture.AuditTestDbContext> _options =
        fixture.CreateOptions();

    private readonly string _connectionString = fixture.ConnectionString;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public async ValueTask InitializeAsync()
    {
        await using var context = new PostgresFixture.AuditTestDbContext(_options);
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await context
            .AuditEntries.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    #region Happy path

    [Fact]
    public async Task ForwardAsync_PublishesThePendingEntryAndMarksItForwarded()
    {
        var entry = await SeedEntryAsync(DateTimeOffset.UtcNow);
        var publisher = new RecordingAuditEventPublisher();

        await RunForwarderAsync(publisher, 1, TestContext.Current.CancellationToken);

        var published = Assert.Single(publisher.Published);
        Assert.Equal(entry.Id, published.EventId);

        await using var context = new PostgresFixture.AuditTestDbContext(_options);
        var stored = await context.AuditEntries.SingleAsync(
            x => x.Id == entry.Id,
            TestContext.Current.CancellationToken
        );
        Assert.NotNull(stored.ForwardedAt);
    }

    [Fact]
    public async Task ForwardAsync_PublishesTheAuditEventFactoryProjection()
    {
        var entry = await SeedEntryAsync(
            DateTimeOffset.UtcNow,
            action: "Clinical.Antibiotic.Created",
            operation: "Created"
        );
        var publisher = new RecordingAuditEventPublisher();

        await RunForwarderAsync(publisher, 1, TestContext.Current.CancellationToken);

        var published = Assert.Single(publisher.Published);
        Assert.Equal(entry.Id, published.EventId);
        Assert.Equal(entry.Action, published.Action);
        Assert.Equal(entry.EntityType, published.EntityType);
        Assert.Equal(entry.ActorRole, published.ActorRole);
        Assert.Equal(entry.EntityId, published.EntityId);

        using var document = JsonDocument.Parse(published.DetailsJson);
        Assert.Equal(entry.Operation, document.RootElement.GetProperty("operation").GetString());
        Assert.Equal(entry.ChangeSetId, document.RootElement.GetProperty("changeSetId").GetGuid());
    }

    [Fact]
    public async Task ForwardAsync_PublishesEveryPendingEntry()
    {
        var first = await SeedEntryAsync(DateTimeOffset.UtcNow.AddMinutes(-3));
        var second = await SeedEntryAsync(DateTimeOffset.UtcNow.AddMinutes(-2));
        var third = await SeedEntryAsync(DateTimeOffset.UtcNow.AddMinutes(-1));
        var publisher = new RecordingAuditEventPublisher();

        await RunForwarderAsync(publisher, 3, TestContext.Current.CancellationToken);

        Assert.Equal(3, publisher.Published.Count);
        Assert.Contains(publisher.Published, x => x.EventId == first.Id);
        Assert.Contains(publisher.Published, x => x.EventId == second.Id);
        Assert.Contains(publisher.Published, x => x.EventId == third.Id);

        await using var context = new PostgresFixture.AuditTestDbContext(_options);
        var forwarded = await context.AuditEntries.ToListAsync(TestContext.Current.CancellationToken);
        Assert.All(forwarded, entry => Assert.NotNull(entry.ForwardedAt));
    }

    [Fact]
    public async Task ForwardAsync_PublishesTheEntriesOrderedByOccurredAt()
    {
        var latest = await SeedEntryAsync(DateTimeOffset.UtcNow.AddMinutes(-1));
        var earliest = await SeedEntryAsync(DateTimeOffset.UtcNow.AddMinutes(-3));
        var middle = await SeedEntryAsync(DateTimeOffset.UtcNow.AddMinutes(-2));
        var publisher = new RecordingAuditEventPublisher();

        await RunForwarderAsync(publisher, 3, TestContext.Current.CancellationToken);

        Assert.Equal(
            new[] { earliest.Id, middle.Id, latest.Id },
            publisher.Published.Select(x => x.EventId)
        );
    }

    [Fact]
    public async Task ForwardAsync_DoesNotPublishAnAlreadyForwardedEntry()
    {
        var alreadyForwarded = await SeedEntryAsync(
            DateTimeOffset.UtcNow.AddMinutes(-2),
            forwardedAt: DateTimeOffset.UtcNow.AddMinutes(-1)
        );
        var pending = await SeedEntryAsync(DateTimeOffset.UtcNow.AddMinutes(-1));
        var publisher = new RecordingAuditEventPublisher();

        await RunForwarderAsync(publisher, 1, TestContext.Current.CancellationToken);

        var published = Assert.Single(publisher.Published);
        Assert.Equal(pending.Id, published.EventId);
        Assert.DoesNotContain(publisher.Published, x => x.EventId == alreadyForwarded.Id);
    }

    #endregion

    #region Publisher failures

    [Fact]
    public async Task ForwardAsync_WhenThePublisherThrows_LeavesTheEntryPending()
    {
        var entry = await SeedEntryAsync(DateTimeOffset.UtcNow);
        var publisher = new RecordingAuditEventPublisher(_ => true);

        await RunForwarderAsync(publisher, OneTick, TestContext.Current.CancellationToken);

        Assert.Empty(publisher.Published);
        Assert.Equal(1, publisher.Failures);

        await using var context = new PostgresFixture.AuditTestDbContext(_options);
        var stored = await context.AuditEntries.SingleAsync(
            x => x.Id == entry.Id,
            TestContext.Current.CancellationToken
        );
        Assert.Null(stored.ForwardedAt);
    }

    [Fact]
    public async Task ForwardAsync_WhenThePublisherThrows_KeepsTheEarlierEntriesForwarded()
    {
        var first = await SeedEntryAsync(DateTimeOffset.UtcNow.AddMinutes(-3));
        var failing = await SeedEntryAsync(DateTimeOffset.UtcNow.AddMinutes(-2));
        var last = await SeedEntryAsync(DateTimeOffset.UtcNow.AddMinutes(-1));
        var publisher = new RecordingAuditEventPublisher(x => x.EventId == failing.Id);

        await RunForwarderAsync(publisher, OneTick, TestContext.Current.CancellationToken);

        var published = Assert.Single(publisher.Published);
        Assert.Equal(first.Id, published.EventId);

        await using var context = new PostgresFixture.AuditTestDbContext(_options);
        var forwardedAt = await context
            .AuditEntries.ToDictionaryAsync(
                x => x.Id,
                x => x.ForwardedAt,
                TestContext.Current.CancellationToken
            );

        Assert.NotNull(forwardedAt[first.Id]);
        Assert.Null(forwardedAt[failing.Id]);
        Assert.Null(forwardedAt[last.Id]);
    }

    [Fact]
    public async Task ForwardAsync_RetriesTheEntryOnTheNextTick()
    {
        var entry = await SeedEntryAsync(DateTimeOffset.UtcNow);
        var attempts = 0;
        var publisher = new RecordingAuditEventPublisher(_ => Interlocked.Increment(ref attempts) == 1);

        await RunForwarderAsync(publisher, 1, TestContext.Current.CancellationToken);

        Assert.Equal(2, attempts);
        var published = Assert.Single(publisher.Published);
        Assert.Equal(entry.Id, published.EventId);

        await using var context = new PostgresFixture.AuditTestDbContext(_options);
        var stored = await context.AuditEntries.SingleAsync(
            x => x.Id == entry.Id,
            TestContext.Current.CancellationToken
        );
        Assert.NotNull(stored.ForwardedAt);
    }

    #endregion

    #region Edge cases

    [Fact]
    public async Task ForwardAsync_WithAnEmptyOutbox_PublishesNothing()
    {
        var publisher = new RecordingAuditEventPublisher();

        await RunForwarderAsync(publisher, OneTick, TestContext.Current.CancellationToken);

        Assert.Empty(publisher.Published);
    }

    [Fact]
    public async Task StopAsync_CompletesWithoutHanging()
    {
        var publisher = new RecordingAuditEventPublisher();
        await using var provider = BuildProvider(publisher);
        var forwarder = CreateForwarder(provider);

        await forwarder.StartAsync(TestContext.Current.CancellationToken);
        var stopTask = forwarder.StopAsync(TestContext.Current.CancellationToken);
        var completed = await Task.WhenAny(
            stopTask,
            Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)
        );

        Assert.Same(stopTask, completed);
        await stopTask;
    }

    #endregion

    private async Task<AuditEntry> SeedEntryAsync(
        DateTimeOffset occurredAt,
        string action = "Test.Entity.Created",
        string operation = "Created",
        DateTimeOffset? forwardedAt = null
    )
    {
        await using var context = new PostgresFixture.AuditTestDbContext(_options);
        var entry = new AuditEntry
        {
            Id = Guid.CreateVersion7(),
            ChangeSetId = Guid.CreateVersion7(),
            OccurredAt = Truncate(occurredAt),
            ActorAccountId = null,
            ActorRole = "System",
            Action = action,
            EntityType = "Entity",
            EntityId = Guid.CreateVersion7(),
            CorrelationId = null,
            Operation = operation,
            BeforeJson = null,
            AfterJson = """{"Name":"Aspirin"}""",
            ChangedFieldsJson = """["Name"]""",
            RequestPath = "/api/v1/entities",
            TraceId = "trace-1",
            ForwardedAt = forwardedAt,
        };

        context.AuditEntries.Add(entry);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return entry;
    }

    private ServiceProvider BuildProvider(RecordingAuditEventPublisher publisher)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<PostgresFixture.AuditTestDbContext>(sp =>
            sp.UseNpgsql(_connectionString)
        );
        services.AddScoped<IAuditEventPublisher>(_ => publisher);
        return services.BuildServiceProvider();
    }

    private static AuditOutboxForwarder<PostgresFixture.AuditTestDbContext> CreateForwarder(
        IServiceProvider provider
    ) =>
        new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<
                ILogger<AuditOutboxForwarder<PostgresFixture.AuditTestDbContext>>
            >()
        );

    private async Task RunForwarderAsync(
        RecordingAuditEventPublisher publisher,
        int expectedPublishedCount,
        CancellationToken cancellationToken
    )
    {
        await using var provider = BuildProvider(publisher);
        var forwarder = CreateForwarder(provider);

        await forwarder.StartAsync(cancellationToken);
        await WaitForPublishedAsync(publisher, expectedPublishedCount, cancellationToken);
        // Give the forwarder a moment to publish anything it should not.
        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        await forwarder.StopAsync(cancellationToken);
    }

    private async Task RunForwarderAsync(
        RecordingAuditEventPublisher publisher,
        TimeSpan duration,
        CancellationToken cancellationToken
    )
    {
        await using var provider = BuildProvider(publisher);
        var forwarder = CreateForwarder(provider);

        await forwarder.StartAsync(cancellationToken);
        await Task.Delay(duration, cancellationToken);
        await forwarder.StopAsync(cancellationToken);
    }

    private static async Task WaitForPublishedAsync(
        RecordingAuditEventPublisher publisher,
        int expectedPublishedCount,
        CancellationToken cancellationToken
    )
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);

        while (DateTime.UtcNow < deadline)
        {
            if (publisher.PublishedCount >= expectedPublishedCount)
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        }
    }

    /// <summary>PostgreSQL stores timestamps with microsecond precision.</summary>
    private static DateTimeOffset Truncate(DateTimeOffset value) =>
        new(value.Ticks - value.Ticks % 10, value.Offset);

    private sealed class RecordingAuditEventPublisher : IAuditEventPublisher
    {
        private readonly object _gate = new();
        private readonly List<AuditEvent> _published = new();
        private readonly Func<AuditEvent, bool>? _shouldFail;

        public RecordingAuditEventPublisher(Func<AuditEvent, bool>? shouldFail = null)
        {
            _shouldFail = shouldFail;
        }

        public int Failures { get; private set; }

        public int PublishedCount
        {
            get
            {
                lock (_gate)
                {
                    return _published.Count;
                }
            }
        }

        public IReadOnlyList<AuditEvent> Published
        {
            get
            {
                lock (_gate)
                {
                    return _published.ToArray();
                }
            }
        }

        public Task PublishAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                if (_shouldFail?.Invoke(auditEvent) == true)
                {
                    Failures++;
                    throw new InvalidOperationException("Simulated publish failure.");
                }

                _published.Add(auditEvent);
            }

            return Task.CompletedTask;
        }
    }
}
