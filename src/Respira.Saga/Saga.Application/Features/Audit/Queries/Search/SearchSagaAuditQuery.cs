using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Saga.Application.Features.Audit.Queries.Search;

public sealed record SearchSagaAuditQuery : IQuery
{
    public Guid? ActorAccountId { get; init; }
    public string? Action { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public PaginationParam Param { get; init; } = new();
}
