namespace Respira.ServiceDefaults.Contracts.Messages.Audit
{
    /// <summary>Security and data-change event retained by the SagaAudit module.</summary>
    public record AuditEvent
    {
        public Guid EventId { get; init; } = Guid.CreateVersion7();
        public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
        public Guid? ActorAccountId { get; init; }
        public string ActorRole { get; init; } = "Service";
        public required string Action { get; init; }
        public required string EntityType { get; init; }
        public Guid? EntityId { get; init; }
        public Guid? CorrelationId { get; init; }
        public string DetailsJson { get; init; } = "{}";
    }
}
