namespace Respira.ServiceDefaults.Auditing;

/// <summary>Identity and request metadata attached to an audit record.</summary>
public sealed record AuditContext(
    Guid? ActorAccountId,
    string ActorRole,
    Guid? CorrelationId,
    string? RequestPath,
    string? TraceId
)
{
    public static readonly AuditContext System = new(null, "System", null, null, null);
}

public interface IAuditContextAccessor
{
    AuditContext GetCurrent();
}

/// <summary>
/// Ambient override for <see cref="AuditContext"/> used by background jobs when no
/// HTTP request is active.
/// </summary>
public static class AuditContextScope
{
    private static readonly AsyncLocal<ScopeState?> State = new();

    public static AuditContext? Current => State.Value?.Context;

    public static Guid? PreferredChangeSetId => State.Value?.PreferredChangeSetId;

    public static IDisposable Begin(
        AuditContext context,
        Guid? preferredAuditEntryId = null,
        Guid? preferredChangeSetId = null
    )
    {
        var previous = State.Value;
        State.Value = new ScopeState(context, preferredAuditEntryId, preferredChangeSetId);
        return new ScopeHandle(previous);
    }

    internal static Guid? ConsumePreferredAuditEntryId()
    {
        var state = State.Value;
        if (state?.PreferredAuditEntryId is not { } id)
            return null;

        state.PreferredAuditEntryId = null;
        return id;
    }

    private sealed class ScopeState(
        AuditContext context,
        Guid? preferredAuditEntryId,
        Guid? preferredChangeSetId
    )
    {
        public AuditContext Context { get; } = context;
        public Guid? PreferredAuditEntryId { get; set; } = preferredAuditEntryId;
        public Guid? PreferredChangeSetId { get; } = preferredChangeSetId;
    }

    private sealed class ScopeHandle(ScopeState? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            State.Value = previous;
        }
    }
}
