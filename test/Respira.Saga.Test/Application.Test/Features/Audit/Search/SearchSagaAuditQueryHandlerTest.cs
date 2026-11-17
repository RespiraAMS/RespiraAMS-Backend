using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Respira.Saga.Application.Features.Audit.Queries.Search;
using Respira.Saga.Application.Features.Audit.Queries.Search.Result;
using Respira.Saga.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Saga.Application.Test.Features.Audit.Search;

public sealed class SearchSagaAuditQueryHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private readonly SearchSagaAuditQueryHandler _handler;

    public SearchSagaAuditQueryHandlerTest(PostgresFixture fixture)
    {
        _fixture = fixture;
        _handler = new SearchSagaAuditQueryHandler(fixture.CreateContext());
    }

    public async ValueTask InitializeAsync()
    {
        await using var dbContext = _fixture.CreateContext();
        await dbContext.AuditLogs.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region Happy path

    [Fact]
    public async Task HandleAsync_WithoutFilters_ReturnsEveryEntry()
    {
        await SeedAsync(CreateEntry("Doctor.ProfileCreated", "Doctor"));
        await SeedAsync(CreateEntry("Authentication.AccountCreated", "Account"));

        var result = await HandleAsync();

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Metadata.TotalItemCount);
        Assert.Equal(2, result.Data.Items.Count());
    }

    [Fact]
    public async Task HandleAsync_FiltersByActorAccountId()
    {
        var actorId = Guid.CreateVersion7();
        var otherActorId = Guid.CreateVersion7();
        await SeedAsync(CreateEntry("Doctor.ProfileCreated", "Doctor", actorId));
        await SeedAsync(CreateEntry("Doctor.ProfileUpdated", "Doctor", otherActorId));

        var result = await HandleAsync(new Filter(ActorAccountId: actorId));

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        var item = Assert.Single(result.Data.Items);
        Assert.Equal(actorId, item.ActorAccountId);
        Assert.Equal(1, result.Data.Metadata.TotalItemCount);
    }

    [Fact]
    public async Task HandleAsync_FiltersByAction()
    {
        await SeedAsync(CreateEntry("Doctor.ProfileCreated", "Doctor"));
        await SeedAsync(CreateEntry("Doctor.ProfileUpdated", "Doctor"));

        var result = await HandleAsync(new Filter(Action: "Doctor.ProfileCreated"));

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        var item = Assert.Single(result.Data.Items);
        Assert.Equal("Doctor.ProfileCreated", item.Action);
    }

    [Fact]
    public async Task HandleAsync_FiltersByEntityType()
    {
        await SeedAsync(CreateEntry("Doctor.ProfileCreated", "Doctor"));
        await SeedAsync(CreateEntry("Account.Created", "Account"));

        var result = await HandleAsync(new Filter(EntityType: "Doctor"));

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        var item = Assert.Single(result.Data.Items);
        Assert.Equal("Doctor", item.EntityType);
    }

    [Fact]
    public async Task HandleAsync_FiltersByEntityId()
    {
        var entityId = Guid.CreateVersion7();
        await SeedAsync(CreateEntry("Doctor.ProfileCreated", "Doctor", entityId: entityId));
        await SeedAsync(CreateEntry("Doctor.ProfileUpdated", "Doctor"));

        var result = await HandleAsync(new Filter(EntityId: entityId));

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        var item = Assert.Single(result.Data.Items);
        Assert.Equal(entityId, item.EntityId);
    }

    [Fact]
    public async Task HandleAsync_FiltersByFromDate()
    {
        var now = DateTimeOffset.UtcNow;
        await SeedAsync(CreateEntry("Old", "Doctor", occurredAt: now.AddHours(-2)));
        await SeedAsync(CreateEntry("New", "Doctor", occurredAt: now));

        var result = await HandleAsync(new Filter(From: now.AddHours(-1)));

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        var item = Assert.Single(result.Data.Items);
        Assert.Equal("New", item.Action);
    }

    [Fact]
    public async Task HandleAsync_FiltersByToDate()
    {
        var now = DateTimeOffset.UtcNow;
        await SeedAsync(CreateEntry("Old", "Doctor", occurredAt: now.AddHours(-2)));
        await SeedAsync(CreateEntry("New", "Doctor", occurredAt: now));

        var result = await HandleAsync(new Filter(To: now.AddHours(-1)));

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        var item = Assert.Single(result.Data.Items);
        Assert.Equal("Old", item.Action);
    }

    [Fact]
    public async Task HandleAsync_CombinesFilters()
    {
        var actorId = Guid.CreateVersion7();
        await SeedAsync(
            CreateEntry("Doctor.ProfileCreated", "Doctor", actorId, entityId: Guid.CreateVersion7())
        );
        await SeedAsync(
            CreateEntry("Doctor.ProfileUpdated", "Doctor", actorId, entityId: Guid.CreateVersion7())
        );
        await SeedAsync(
            CreateEntry("Account.Created", "Account", Guid.CreateVersion7(), Guid.CreateVersion7())
        );

        var result = await HandleAsync(new Filter(ActorAccountId: actorId, Action: "Doctor.ProfileCreated"));

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal("Doctor.ProfileCreated", Assert.Single(result.Data.Items).Action);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNewestEntryFirst()
    {
        var now = DateTimeOffset.UtcNow;
        await SeedAsync(CreateEntry("Oldest", "Doctor", occurredAt: now.AddMinutes(-3)));
        await SeedAsync(CreateEntry("Middle", "Doctor", occurredAt: now.AddMinutes(-2)));
        await SeedAsync(CreateEntry("Newest", "Doctor", occurredAt: now.AddMinutes(-1)));

        var result = await HandleAsync();

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        var actions = result.Data.Items.Select(item => item.Action).ToArray();
        Assert.Equal(["Newest", "Middle", "Oldest"], actions);
    }

    [Fact]
    public async Task HandleAsync_PaginatesTheResults()
    {
        var now = DateTimeOffset.UtcNow;
        for (var index = 0; index < 5; index++)
        {
            await SeedAsync(
                CreateEntry($"Action-{index}", "Doctor", occurredAt: now.AddMinutes(index))
            );
        }

        var result = await HandleAsync(new Filter(Page: 1, Size: 2));

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Items.Count());
        Assert.Equal(5, result.Data.Metadata.TotalItemCount);
        Assert.Equal(3, result.Data.Metadata.PageCount);
        Assert.Equal(1, result.Data.Metadata.CurrentPage);
        Assert.Equal(2, result.Data.Metadata.PageSize);
        Assert.True(result.Data.Metadata.HasNextPage);
        Assert.False(result.Data.Metadata.HasPreviousPage);
    }

    [Fact]
    public async Task HandleAsync_SecondPage_ReturnsTheNextSlice()
    {
        var now = DateTimeOffset.UtcNow;
        for (var index = 0; index < 5; index++)
        {
            await SeedAsync(
                CreateEntry($"Action-{index}", "Doctor", occurredAt: now.AddMinutes(index))
            );
        }

        var result = await HandleAsync(new Filter(Page: 2, Size: 2));

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Items.Count());
        Assert.Equal(2, result.Data.Metadata.CurrentPage);
        Assert.True(result.Data.Metadata.HasPreviousPage);
        Assert.True(result.Data.Metadata.HasNextPage);
    }

    [Fact]
    public async Task HandleAsync_LastPage_HasNoNextPage()
    {
        var now = DateTimeOffset.UtcNow;
        for (var index = 0; index < 5; index++)
        {
            await SeedAsync(
                CreateEntry($"Action-{index}", "Doctor", occurredAt: now.AddMinutes(index))
            );
        }

        var result = await HandleAsync(new Filter(Page: 3, Size: 2));

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        var item = Assert.Single(result.Data.Items);
        Assert.Equal("Action-0", item.Action);
        Assert.True(result.Data.Metadata.HasPreviousPage);
        Assert.False(result.Data.Metadata.HasNextPage);
    }

    [Fact]
    public async Task HandleAsync_ProjectsEveryFieldOfTheEntry()
    {
        var actorId = Guid.CreateVersion7();
        var entityId = Guid.CreateVersion7();
        var correlationId = Guid.CreateVersion7();
        var occurredAt = DateTimeOffset.UtcNow;
        await SeedAsync(
            new AuditLogEntry
            {
                Id = Guid.CreateVersion7(),
                OccurredAt = occurredAt,
                ActorAccountId = actorId,
                ActorRole = "Admin",
                Action = "Doctor.ProfileCreated",
                EntityType = "Doctor",
                EntityId = entityId,
                CorrelationId = correlationId,
                DetailsJson = "{\"source\":\"test\"}",
            }
        );

        var result = await HandleAsync();

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        var item = Assert.Single(result.Data.Items);
        Assert.Equal(actorId, item.ActorAccountId);
        Assert.Equal("Admin", item.ActorRole);
        Assert.Equal("Doctor.ProfileCreated", item.Action);
        Assert.Equal("Doctor", item.EntityType);
        Assert.Equal(entityId, item.EntityId);
        Assert.Equal(correlationId, item.CorrelationId);
        // The column is jsonb, so PostgreSQL normalizes the whitespace of the payload.
        using (var expected = JsonDocument.Parse("{\"source\":\"test\"}"))
        using (var actual = JsonDocument.Parse(item.DetailsJson))
        {
            Assert.Equal(
                expected.RootElement.GetProperty("source").GetString(),
                actual.RootElement.GetProperty("source").GetString()
            );
        }
        Assert.Equal(
            occurredAt.ToUnixTimeMilliseconds(),
            item.OccurredAt.ToUnixTimeMilliseconds()
        );
    }

    #endregion

    #region Edge cases

    [Fact]
    public async Task HandleAsync_WithNoEntries_ReturnsEmptyPage()
    {
        var result = await HandleAsync();

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data.Items);
        Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        Assert.False(result.Data.Metadata.HasNextPage);
        Assert.False(result.Data.Metadata.HasPreviousPage);
    }

    [Fact]
    public async Task HandleAsync_WithBlankAction_IgnoresTheFilter()
    {
        await SeedAsync(CreateEntry("Doctor.ProfileCreated", "Doctor"));

        var result = await HandleAsync(new Filter(Action: "   "));

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
    }

    [Fact]
    public async Task HandleAsync_WithBlankEntityType_IgnoresTheFilter()
    {
        await SeedAsync(CreateEntry("Doctor.ProfileCreated", "Doctor"));

        var result = await HandleAsync(new Filter(EntityType: ""));

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
    }

    [Fact]
    public async Task HandleAsync_WithAFilterThatMatchesNothing_ReturnsEmptyPage()
    {
        await SeedAsync(CreateEntry("Doctor.ProfileCreated", "Doctor"));

        var result = await HandleAsync(new Filter(Action: "Does.Not.Exist"));

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data.Items);
        Assert.Equal(0, result.Data.Metadata.TotalItemCount);
    }

    [Fact]
    public async Task HandleAsync_WithPageBeyondTheLastOne_ReturnsEmptyPage()
    {
        await SeedAsync(CreateEntry("Doctor.ProfileCreated", "Doctor"));

        var result = await HandleAsync(new Filter(Page: 5, Size: 10));

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data.Items);
        Assert.Equal(1, result.Data.Metadata.TotalItemCount);
        Assert.False(result.Data.Metadata.HasNextPage);
    }

    [Fact]
    public async Task HandleAsync_WithEqualFromAndTo_IncludesTheBoundary()
    {
        var occurredAt = DateTimeOffset.UtcNow;
        await SeedAsync(CreateEntry("Edge", "Doctor", occurredAt: occurredAt));

        var result = await HandleAsync(new Filter(From: occurredAt, To: occurredAt));

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
    }

    [Fact]
    public async Task HandleAsync_WithANullQuery_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _handler.HandleAsync(null!, TestContext.Current.CancellationToken)
        );
    }

    #endregion

    /// <summary>
    /// The query record exposes init-only properties, so the filters travel through
    /// this mutable carrier and are copied into the query in one place.
    /// </summary>
    private sealed record Filter(
        Guid? ActorAccountId = null,
        string? Action = null,
        string? EntityType = null,
        Guid? EntityId = null,
        DateTimeOffset? From = null,
        DateTimeOffset? To = null,
        int Page = 1,
        int Size = 10
    );

    private async Task<Result<Pagination<SagaAuditResult>>> HandleAsync(Filter? filter = null)
    {
        var filterToApply = filter ?? new Filter();
        var query = new SearchSagaAuditQuery
        {
            ActorAccountId = filterToApply.ActorAccountId,
            Action = filterToApply.Action,
            EntityType = filterToApply.EntityType,
            EntityId = filterToApply.EntityId,
            From = filterToApply.From,
            To = filterToApply.To,
            Param = new PaginationParam { Page = filterToApply.Page, Size = filterToApply.Size },
        };

        return await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
    }
    private async Task SeedAsync(AuditLogEntry entry)
    {
        await using var dbContext = _fixture.CreateContext();
        dbContext.AuditLogs.Add(entry);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static AuditLogEntry CreateEntry(
        string action,
        string entityType,
        Guid? actorAccountId = null,
        Guid? entityId = null,
        DateTimeOffset? occurredAt = null
    ) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            OccurredAt = occurredAt ?? DateTimeOffset.UtcNow,
            ActorAccountId = actorAccountId ?? Guid.CreateVersion7(),
            ActorRole = "Admin",
            Action = action,
            EntityType = entityType,
            EntityId = entityId ?? Guid.CreateVersion7(),
            CorrelationId = Guid.CreateVersion7(),
            DetailsJson = "{}",
        };
}
