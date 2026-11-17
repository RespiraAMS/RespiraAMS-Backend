namespace Respira.Saga.Application.Features.Audit.Queries.Search.Result;

public sealed class SagaAuditResult
{
    public Guid Id { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public Guid? ActorAccountId { get; init; }
    public string ActorRole { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public Guid? EntityId { get; init; }
    public Guid? CorrelationId { get; init; }
    public string DetailsJson { get; init; } = "{}";
}
