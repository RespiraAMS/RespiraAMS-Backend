using Respira.ServiceDefaults.Contracts.Messages.Audit;

namespace Respira.ServiceDefaults.Auditing;

/// <summary>
/// Service-specific transport adapter for the shared Saga audit event.
/// </summary>
public interface IAuditEventPublisher
{
    Task PublishAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
