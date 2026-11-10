using Respira.ServiceDefaults.Auditing;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Wolverine;

namespace Respira.Analysis.API.Auditing;

public sealed class AnalysisAuditEventPublisher(IMessageBus bus) : IAuditEventPublisher
{
    public async Task PublishAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken = default
    )
    {
        await bus.PublishAsync(auditEvent);
    }
}
