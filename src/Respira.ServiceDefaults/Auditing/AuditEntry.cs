namespace Respira.ServiceDefaults.Auditing;

/// <summary>
/// Durable, service-local audit record.
///
/// Every microservice that owns a database writes one row per entity change to
/// its own <c>audit_entries</c> table in the same transaction as the business
/// change. A background forwarder publishes the row to the centralized Saga
/// audit store. Keeping the first copy local means the audit trail is not lost
/// when RabbitMQ or the Saga service is temporarily unavailable.
/// </summary>
public sealed class AuditEntry
{
    /// <summary>Event identifier. Also the idempotency key in the central store.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Identifier shared by every row written during one SaveChanges call.</summary>
    public Guid ChangeSetId { get; set; } = Guid.CreateVersion7();

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Actor that caused the change. Null for background/system work.</summary>
    public Guid? ActorAccountId { get; set; }

    /// <summary>Actor role at the time of the change, or <c>System</c>.</summary>
    public string ActorRole { get; set; } = "System";

    /// <summary>Stable action name, e.g. <c>Clinical.Antibiotic.Created</c>.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Logical entity name, e.g. <c>Antibiotic</c>. Join tables use <c>__join__:table</c>.</summary>
    public string EntityType { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    /// <summary>Correlation id supplied by the caller, when available.</summary>
    public Guid? CorrelationId { get; set; }

    /// <summary>Created, Updated, SoftDeleted, Restored, Deleted or Rollback.</summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>JSON snapshot before the change. Null for inserts.</summary>
    public string? BeforeJson { get; set; }

    /// <summary>JSON snapshot after the change. Null for hard deletes.</summary>
    public string? AfterJson { get; set; }

    /// <summary>JSON array with the names of the properties that changed.</summary>
    public string ChangedFieldsJson { get; set; } = "[]";

    /// <summary>HTTP path that triggered the change, when applicable.</summary>
    public string? RequestPath { get; set; }

    /// <summary>Distributed trace id, for correlating logs and audit records.</summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Operational metadata: when the record was forwarded to the central store.
    /// This is not part of the audited payload.
    /// </summary>
    public DateTimeOffset? ForwardedAt { get; set; }
}
