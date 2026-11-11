using Respira.ServiceDefaults.Auditing;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Wolverine;

namespace Respira.Patient.API.Auditing;

public sealed class PatientAuditEventPublisher(IMessageBus bus) : IAuditEventPublisher
{
    public async Task PublishAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken = default
    )
    {
        await bus.PublishAsync(auditEvent);
    }
}
