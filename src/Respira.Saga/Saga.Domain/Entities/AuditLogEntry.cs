namespace Respira.Saga.Domain.Entities;

public sealed class AuditLogEntry
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public Guid? ActorAccountId { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public Guid? CorrelationId { get; set; }
    public string DetailsJson { get; set; } = "{}";
}
