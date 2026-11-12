using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
using Moq;
using Npgsql;
using Respira.Saga.Application.Features.Audit.Commands.Record;
using Respira.Saga.Application.Features.Audit.Queries.Search;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Saga.Application.Test.Features.Audit.Record;

public sealed class RecordSagaAuditEventHandlerTest(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly ILogger<RecordSagaAuditEventHandler> _logger =
        Mock.Of<ILogger<RecordSagaAuditEventHandler>>();

    public async ValueTask InitializeAsync()
    {
        await using var dbContext = fixture.CreateContext();
        await dbContext.AuditLogs.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task HandleAsync_PersistsAuditEvent()
    {
        var auditEvent = CreateEvent();
        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);
            await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = fixture.CreateContext();
        var saved = await verificationContext.AuditLogs.SingleAsync(
            item => item.Id == auditEvent.EventId,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(auditEvent.Action, saved.Action);
        Assert.Equal(auditEvent.ActorAccountId, saved.ActorAccountId);
        Assert.Equal(auditEvent.EntityId, saved.EntityId);
        using var expectedDetails = JsonDocument.Parse(auditEvent.DetailsJson);
        using var savedDetails = JsonDocument.Parse(saved.DetailsJson);
        Assert.Equal(
            expectedDetails.RootElement.GetProperty("source").GetString(),
            savedDetails.RootElement.GetProperty("source").GetString()
        );
    }

    [Fact]
    public async Task HandleAsync_DuplicateEvent_IsIdempotent()
    {
        var auditEvent = CreateEvent();
        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);
            await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
            await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = fixture.CreateContext();
        Assert.Equal(1, await verificationContext.AuditLogs.CountAsync(
            item => item.Id == auditEvent.EventId,
            TestContext.Current.CancellationToken
        ));
    }

    [Fact]
    public async Task SearchHandler_AppliesFiltersAndPagination()
    {
        var actorId = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow;
        await using (var seedContext = fixture.CreateContext())
        {
            seedContext.AuditLogs.AddRange(
                CreateEntry(actorId, "Authentication.AccountCreated", now.AddMinutes(-3)),
                CreateEntry(actorId, "Authentication.AccountUpdated", now.AddMinutes(-2)),
                CreateEntry(Guid.CreateVersion7(), "Authentication.AccountCreated", now.AddMinutes(-1))
            );
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var queryContext = fixture.CreateContext();
        var handler = new SearchSagaAuditQueryHandler(queryContext);
        var result = await handler.HandleAsync(new SearchSagaAuditQuery
        {
            ActorAccountId = actorId,
            Param = new PaginationParam { Page = 1, Size = 1 },
        }, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Metadata.TotalItemCount);
        Assert.True(result.Data.Metadata.HasNextPage);
        Assert.Single(result.Data.Items);
        Assert.Equal("Authentication.AccountUpdated", result.Data.Items.First().Action);
    }

    [Fact]
    public async Task HandleAsync_PersistsEveryFieldOfTheEvent()
    {
        var auditEvent = CreateEvent() with
        {
            ActorRole = "HeadDepartmentDoctor",
            CorrelationId = Guid.CreateVersion7(),
        };

        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);
            await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = fixture.CreateContext();
        var saved = await verificationContext.AuditLogs.SingleAsync(
            item => item.Id == auditEvent.EventId,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(auditEvent.EventId, saved.Id);
        Assert.Equal(auditEvent.OccurredAt.ToUnixTimeMilliseconds(), saved.OccurredAt.ToUnixTimeMilliseconds());
        Assert.Equal(auditEvent.ActorAccountId, saved.ActorAccountId);
        Assert.Equal("HeadDepartmentDoctor", saved.ActorRole);
        Assert.Equal(auditEvent.Action, saved.Action);
        Assert.Equal(auditEvent.EntityType, saved.EntityType);
        Assert.Equal(auditEvent.EntityId, saved.EntityId);
        Assert.Equal(auditEvent.CorrelationId, saved.CorrelationId);
    }

    [Fact]
    public async Task HandleAsync_WithNullActorAccountId_PersistsANullActor()
    {
        var auditEvent = CreateEvent() with
        {
            ActorAccountId = null,
            EntityId = null,
            CorrelationId = null,
        };

        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);
            await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = fixture.CreateContext();
        var saved = await verificationContext.AuditLogs.SingleAsync(
            item => item.Id == auditEvent.EventId,
            TestContext.Current.CancellationToken
        );

        Assert.Null(saved.ActorAccountId);
        Assert.Null(saved.EntityId);
        Assert.Null(saved.CorrelationId);
    }

    [Fact]
    public async Task HandleAsync_WithADifferentEventId_StoresASeparateRow()
    {
        var first = CreateEvent();
        var second = CreateEvent();

        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);
            await handler.HandleAsync(first, TestContext.Current.CancellationToken);
            await handler.HandleAsync(second, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = fixture.CreateContext();
        Assert.Equal(
            2,
            await verificationContext.AuditLogs.CountAsync(TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task HandleAsync_WithTheSameEventIdButDifferentContent_KeepsTheFirstRow()
    {
        var auditEvent = CreateEvent();
        var mutated = auditEvent with { Action = "Doctor.ProfileUpdated" };

        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);
            await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
            await handler.HandleAsync(mutated, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = fixture.CreateContext();
        var saved = await verificationContext.AuditLogs.SingleAsync(
            item => item.Id == auditEvent.EventId,
            TestContext.Current.CancellationToken
        );
        Assert.Equal("Doctor.ProfileCreated", saved.Action);
    }

    [Fact]
    public async Task HandleAsync_WithABlankAction_StoresItAsIs()
    {
        var auditEvent = CreateEvent() with { Action = string.Empty };

        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);
            await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = fixture.CreateContext();
        var saved = await verificationContext.AuditLogs.SingleAsync(
            item => item.Id == auditEvent.EventId,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(string.Empty, saved.Action);
    }

    [Fact]
    public async Task HandleAsync_WithABlankActorRole_StoresItAsIs()
    {
        var auditEvent = CreateEvent() with { ActorRole = string.Empty };

        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);
            await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = fixture.CreateContext();
        var saved = await verificationContext.AuditLogs.SingleAsync(
            item => item.Id == auditEvent.EventId,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(string.Empty, saved.ActorRole);
    }

    [Fact]
    public async Task HandleAsync_WithABlankEntityType_StoresItAsIs()
    {
        var auditEvent = CreateEvent() with { EntityType = string.Empty };

        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);
            await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = fixture.CreateContext();
        var saved = await verificationContext.AuditLogs.SingleAsync(
            item => item.Id == auditEvent.EventId,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(string.Empty, saved.EntityType);
    }

    [Fact]
    public async Task HandleAsync_WithABlankDetailsJson_IsRejectedByTheJsonColumn()
    {
        var auditEvent = CreateEvent() with { DetailsJson = string.Empty };

        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);

            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken)
            );

            var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
            Assert.Equal("22P02", postgresException.SqlState);
        }

        await using var verificationContext = fixture.CreateContext();
        Assert.Empty(await verificationContext.AuditLogs
            .Where(item => item.Id == auditEvent.EventId)
            .ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_WithAnActionAtTheColumnLimit_StoresItInFull()
    {
        var action = new string('a', 128);
        var auditEvent = CreateEvent() with { Action = action };

        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);
            await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = fixture.CreateContext();
        var saved = await verificationContext.AuditLogs.SingleAsync(
            item => item.Id == auditEvent.EventId,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(128, saved.Action.Length);
        Assert.Equal(action, saved.Action);
    }

    [Fact]
    public async Task HandleAsync_WithAnActionLongerThanTheColumnLimit_IsRejectedByTheDatabase()
    {
        var auditEvent = CreateEvent() with { Action = new string('a', 200) };

        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);

            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken)
            );

            var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
            Assert.Equal("22001", postgresException.SqlState);
        }

        await using var verificationContext = fixture.CreateContext();
        Assert.Empty(await verificationContext.AuditLogs
            .Where(item => item.Id == auditEvent.EventId)
            .ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_WithAnActorRoleAtTheColumnLimit_StoresItInFull()
    {
        var actorRole = new string('b', 64);
        var auditEvent = CreateEvent() with { ActorRole = actorRole };

        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);
            await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = fixture.CreateContext();
        var saved = await verificationContext.AuditLogs.SingleAsync(
            item => item.Id == auditEvent.EventId,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(64, saved.ActorRole.Length);
        Assert.Equal(actorRole, saved.ActorRole);
    }

    [Fact]
    public async Task HandleAsync_WithAnActorRoleLongerThanTheColumnLimit_IsRejectedByTheDatabase()
    {
        var auditEvent = CreateEvent() with { ActorRole = new string('b', 100) };

        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);

            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken)
            );

            var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
            Assert.Equal("22001", postgresException.SqlState);
        }

        await using var verificationContext = fixture.CreateContext();
        Assert.Empty(await verificationContext.AuditLogs
            .Where(item => item.Id == auditEvent.EventId)
            .ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_WithNestedDetailsJson_RoundTripsSemantically()
    {
        const string details =
            "{\"profile\":{\"name\":\"Ada\",\"roles\":[\"doctor\",\"reviewer\"]},"
            + "\"meta\":{\"tags\":[{\"k\":\"v\"}],\"active\":true},\"count\":3}";
        var auditEvent = CreateEvent() with { DetailsJson = details };

        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);
            await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = fixture.CreateContext();
        var saved = await verificationContext.AuditLogs.SingleAsync(
            item => item.Id == auditEvent.EventId,
            TestContext.Current.CancellationToken
        );

        // The jsonb column normalizes the whitespace, so the documents are compared semantically.
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(details), JsonNode.Parse(saved.DetailsJson)));

        using var document = JsonDocument.Parse(saved.DetailsJson);
        var root = document.RootElement;
        Assert.Equal("Ada", root.GetProperty("profile").GetProperty("name").GetString());
        Assert.Equal(2, root.GetProperty("profile").GetProperty("roles").GetArrayLength());
        Assert.Equal("v", root.GetProperty("meta").GetProperty("tags")[0].GetProperty("k").GetString());
        Assert.True(root.GetProperty("meta").GetProperty("active").GetBoolean());
        Assert.Equal(3, root.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task HandleAsync_WithFiveDifferentEvents_StoresFiveRows()
    {
        var events = Enumerable.Range(0, 5)
            .Select(index => CreateEvent() with { Action = $"Doctor.ProfileCreated{index}" })
            .ToList();

        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);
            foreach (var auditEvent in events)
                await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = fixture.CreateContext();
        Assert.Equal(5, await verificationContext.AuditLogs.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_WithFiveEventsSharingTheSameId_StoresExactlyOneRow()
    {
        var eventId = Guid.CreateVersion7();
        var events = Enumerable.Range(0, 5)
            .Select(index => CreateEvent() with
            {
                EventId = eventId,
                Action = $"Doctor.ProfileCreated{index}",
            })
            .ToList();

        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);
            foreach (var auditEvent in events)
                await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = fixture.CreateContext();
        var saved = await verificationContext.AuditLogs.SingleAsync(
            item => item.Id == eventId,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(1, await verificationContext.AuditLogs.CountAsync(
            item => item.Id == eventId,
            TestContext.Current.CancellationToken
        ));
        Assert.Equal("Doctor.ProfileCreated0", saved.Action);
    }

    [Fact]
    public async Task HandleAsync_PreservesTheOccurredAtTimestampToMillisecondPrecision()
    {
        var occurredAt = new DateTimeOffset(2026, 3, 14, 15, 9, 26, 535, TimeSpan.Zero);
        var auditEvent = CreateEvent() with { OccurredAt = occurredAt };

        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);
            await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = fixture.CreateContext();
        var saved = await verificationContext.AuditLogs.SingleAsync(
            item => item.Id == auditEvent.EventId,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(occurredAt.ToUnixTimeMilliseconds(), saved.OccurredAt.ToUnixTimeMilliseconds());
        Assert.Equal(occurredAt, saved.OccurredAt);
    }

    [Fact]
    public async Task HandleAsync_PreservesTheCorrelationId()
    {
        var correlationId = Guid.CreateVersion7();
        var auditEvent = CreateEvent() with { CorrelationId = correlationId };

        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);
            await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = fixture.CreateContext();
        var saved = await verificationContext.AuditLogs.SingleAsync(
            item => item.Id == auditEvent.EventId,
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(saved.CorrelationId);
        Assert.Equal(correlationId, saved.CorrelationId.Value);
    }

    [Fact]
    public async Task SearchHandler_FindsTheRecordedRowByEntityType()
    {
        var auditEvent = CreateEvent() with { EntityType = "Media" };

        await using (var dbContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(dbContext, _logger);
            await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
        }

        await using var queryContext = fixture.CreateContext();
        var searchHandler = new SearchSagaAuditQueryHandler(queryContext);
        var result = await searchHandler.HandleAsync(new SearchSagaAuditQuery
        {
            EntityType = "Media",
            Param = new PaginationParam { Page = 1, Size = 10 },
        }, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.Metadata.TotalItemCount);
        var item = Assert.Single(result.Data.Items);
        Assert.Equal(auditEvent.EventId, item.Id);
        Assert.Equal("Media", item.EntityType);
        Assert.Equal(auditEvent.Action, item.Action);
    }

    [Fact]
    public async Task HandleAsync_WithANewHandlerInstanceAndTheSameEventId_DoesNotDuplicateTheRow()
    {
        var auditEvent = CreateEvent();

        await using (var firstContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(firstContext, _logger);
            await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
        }

        await using (var secondContext = fixture.CreateContext())
        {
            var handler = new RecordSagaAuditEventHandler(secondContext, _logger);
            await handler.HandleAsync(auditEvent, TestContext.Current.CancellationToken);
        }

        await using var verificationContext = fixture.CreateContext();
        Assert.Equal(1, await verificationContext.AuditLogs.CountAsync(
            item => item.Id == auditEvent.EventId,
            TestContext.Current.CancellationToken
        ));
    }

    private static AuditEvent CreateEvent() => new()
    {
        ActorAccountId = Guid.CreateVersion7(),
        ActorRole = "Admin",
        Action = "Doctor.ProfileCreated",
        EntityType = "Doctor",
        EntityId = Guid.CreateVersion7(),
        DetailsJson = "{\"source\":\"test\"}",
    };

    private static Respira.Saga.Domain.Entities.AuditLogEntry CreateEntry(
        Guid actorAccountId,
        string action,
        DateTimeOffset occurredAt
    ) => new()
    {
        Id = Guid.CreateVersion7(),
        OccurredAt = occurredAt,
        ActorAccountId = actorAccountId,
        ActorRole = "Admin",
        Action = action,
        EntityType = "Account",
        DetailsJson = "{}",
    };
}
