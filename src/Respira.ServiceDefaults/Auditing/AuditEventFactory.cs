using System.Text.Json;
using Respira.ServiceDefaults.Contracts.Messages.Audit;

namespace Respira.ServiceDefaults.Auditing;

/// <summary>Maps the local audit outbox row to Saga's shared audit event contract.</summary>
public static class AuditEventFactory
{
    public static AuditEvent Create(AuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var details = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["operation"] = entry.Operation,
            ["changeSetId"] = entry.ChangeSetId,
            ["changedFields"] = Parse(entry.ChangedFieldsJson),
            ["before"] = Parse(entry.BeforeJson),
            ["after"] = Parse(entry.AfterJson),
            ["requestPath"] = entry.RequestPath,
            ["traceId"] = entry.TraceId,
        };

        return new AuditEvent
        {
            EventId = entry.Id,
            OccurredAt = entry.OccurredAt,
            ActorAccountId = entry.ActorAccountId,
            ActorRole = entry.ActorRole,
            Action = entry.Action,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            CorrelationId = entry.CorrelationId,
            DetailsJson = JsonSerializer.Serialize(details, AuditSnapshot.Options),
        };
    }

    private static JsonElement? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
