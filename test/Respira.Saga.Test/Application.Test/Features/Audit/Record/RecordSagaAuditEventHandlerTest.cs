using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Moq;
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
