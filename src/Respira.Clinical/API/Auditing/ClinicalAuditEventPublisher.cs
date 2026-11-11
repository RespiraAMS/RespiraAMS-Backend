using Respira.ServiceDefaults.Auditing;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Wolverine;

namespace Respira.Clinical.API.Auditing;

public sealed class ClinicalAuditEventPublisher(IMessageBus bus) : IAuditEventPublisher
{
    public async Task PublishAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken = default
    )
    {
        await bus.PublishAsync(auditEvent);
    }
}
