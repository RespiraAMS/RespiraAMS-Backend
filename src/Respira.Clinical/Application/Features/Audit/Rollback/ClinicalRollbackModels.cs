namespace Respira.Clinical.Application.Features.Audit.Rollback;

public sealed record ClinicalRollbackChange(
    string EntityType,
    Guid? EntityId,
    string Operation,
    string Description,
    string? CurrentJson,
    string? TargetJson,
    bool IsJoinTable
);

public sealed record ClinicalRollbackConflict(
    string EntityType,
    Guid? EntityId,
    string Reason
);

public sealed record ClinicalRollbackPreview(
    Guid AuditEntryId,
    Guid ChangeSetId,
    string Action,
    DateTimeOffset OccurredAt,
    IReadOnlyList<ClinicalRollbackChange> Changes,
    IReadOnlyList<ClinicalRollbackConflict> Conflicts
)
{
    public bool CanApply => Conflicts.Count == 0;
}

public sealed record ClinicalRollbackResult(
    Guid AuditEntryId,
    Guid ChangeSetId,
    int ChangesApplied,
    Guid RollbackAuditEntryId,
    DateTimeOffset CompletedAt
);

public sealed record ClinicalAuditEntryResult(
    Guid Id,
    Guid ChangeSetId,
    DateTimeOffset OccurredAt,
    Guid? ActorAccountId,
    string ActorRole,
    string Action,
    string EntityType,
    Guid? EntityId,
    Guid? CorrelationId,
    string Operation,
    string? BeforeJson,
    string? AfterJson,
    string ChangedFieldsJson,
    string? RequestPath,
    string? TraceId,
    DateTimeOffset? ForwardedAt
);

public sealed class ClinicalRollbackConflictException(ClinicalRollbackPreview preview)
    : Exception("The current data no longer matches the audited state")
{
    public ClinicalRollbackPreview Preview { get; } = preview;
}
