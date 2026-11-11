namespace Respira.Clinical.Application.Features.Audit.Rollback;

public interface IClinicalRollbackService
{
    Task<ClinicalAuditEntryResult?> GetAsync(
        Guid auditEntryId,
        CancellationToken cancellationToken = default
    );

    Task<ClinicalRollbackPreview?> PreviewAsync(
        Guid auditEntryId,
        CancellationToken cancellationToken = default
    );

    Task<ClinicalRollbackResult?> ApplyAsync(
        Guid auditEntryId,
        CancellationToken cancellationToken = default
    );
}
