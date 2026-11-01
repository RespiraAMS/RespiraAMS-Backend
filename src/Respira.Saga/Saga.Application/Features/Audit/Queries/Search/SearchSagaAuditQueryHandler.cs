using Microsoft.EntityFrameworkCore;
using Respira.Saga.Application.Constracts.Data;
using Respira.Saga.Application.Features.Audit.Queries.Search.Result;
using Respira.Saga.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Saga.Application.Features.Audit.Queries.Search;

public sealed class SearchSagaAuditQueryHandler(ISagaAuditDbContext dbContext)
    : IQueryHandler<SearchSagaAuditQuery, Result<Pagination<SagaAuditResult>>>
{
    public async Task<Result<Pagination<SagaAuditResult>>> HandleAsync(
        SearchSagaAuditQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        IQueryable<AuditLogEntry> entries = dbContext.AuditLogs.AsNoTracking();
        if (query.ActorAccountId.HasValue)
            entries = entries.Where(entry => entry.ActorAccountId == query.ActorAccountId.Value);
        if (!string.IsNullOrWhiteSpace(query.Action))
            entries = entries.Where(entry => entry.Action == query.Action);
        if (!string.IsNullOrWhiteSpace(query.EntityType))
            entries = entries.Where(entry => entry.EntityType == query.EntityType);
        if (query.EntityId.HasValue)
            entries = entries.Where(entry => entry.EntityId == query.EntityId.Value);
        if (query.From.HasValue)
            entries = entries.Where(entry => entry.OccurredAt >= query.From.Value);
        if (query.To.HasValue)
            entries = entries.Where(entry => entry.OccurredAt <= query.To.Value);

        var total = await entries.CountAsync(cancellationToken);
        var items = await entries.OrderByDescending(entry => entry.OccurredAt)
            .Skip((query.Param.Page - 1) * query.Param.Size)
            .Take(query.Param.Size)
            .Select(entry => new SagaAuditResult
            {
                Id = entry.Id,
                OccurredAt = entry.OccurredAt,
                ActorAccountId = entry.ActorAccountId,
                ActorRole = entry.ActorRole,
                Action = entry.Action,
                EntityType = entry.EntityType,
                EntityId = entry.EntityId,
                CorrelationId = entry.CorrelationId,
                DetailsJson = entry.DetailsJson,
            })
            .ToListAsync(cancellationToken);

        var pages = (int)Math.Ceiling(total / (double)query.Param.Size);
        var metadata = new PaginationMetadata
        {
            CurrentPage = query.Param.Page,
            PageSize = query.Param.Size,
            TotalItemCount = total,
            PageCount = pages,
            HasPreviousPage = query.Param.Page > 1 && total > 0,
            HasNextPage = query.Param.Page < pages,
        };
        return Result<Pagination<SagaAuditResult>>.Success(
            ApplicationStatus.Success,
            new Pagination<SagaAuditResult>(metadata, items)
        );
    }
}
