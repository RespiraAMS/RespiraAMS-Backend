using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Saga.Application.Constracts.Data;
using Respira.Saga.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Messages.Audit;

namespace Respira.Saga.Application.Features.Audit.Commands.Record;

public sealed class RecordSagaAuditEventHandler(
    ISagaAuditDbContext dbContext,
    ILogger<RecordSagaAuditEventHandler> logger
)
{
    public async Task HandleAsync(AuditEvent message, CancellationToken cancellationToken)
    {
        if (await dbContext.AuditLogs.AnyAsync(entry => entry.Id == message.EventId, cancellationToken))
            return;

        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = message.EventId,
            OccurredAt = message.OccurredAt,
            ActorAccountId = message.ActorAccountId,
            ActorRole = message.ActorRole,
            Action = message.Action,
            EntityType = message.EntityType,
            EntityId = message.EntityId,
            CorrelationId = message.CorrelationId,
            DetailsJson = message.DetailsJson,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Stored audit event {EventId} {Action} for {EntityType} {EntityId}",
            message.EventId,
            message.Action,
            message.EntityType,
            message.EntityId
        );
    }
}
