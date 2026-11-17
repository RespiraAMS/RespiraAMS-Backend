using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Respira.ServiceDefaults.Auditing;

/// <summary>
/// DbContext base that writes a durable local audit row for every entity change.
/// Audit rows are added to the same change tracker as the business change, so
/// they commit or roll back together with it.
/// </summary>
public abstract class AuditableDbContext : DbContext
{
    private readonly string _serviceName;
    private readonly IAuditContextAccessor? _auditContextAccessor;
    private bool _capturing;
    private int _suppressCaptureDepth;

    protected AuditableDbContext(
        DbContextOptions options,
        string serviceName,
        IAuditContextAccessor? auditContextAccessor = null
    )
        : base(options)
    {
        _serviceName = serviceName;
        _auditContextAccessor = auditContextAccessor;
    }

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <summary>
    /// Temporarily disables automatic audit entry generation during SaveChanges,
    /// e.g. when applying a rollback that records its own explicit Rollback entry.
    /// </summary>
    public IDisposable SuppressAuditCapture()
    {
        _suppressCaptureDepth++;
        return new SuppressCaptureScope(this);
    }

    private sealed class SuppressCaptureScope(AuditableDbContext context) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            context._suppressCaptureDepth--;
        }
    }

    public override int SaveChanges() => SaveChanges(true);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        CaptureAuditEntries();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(true, cancellationToken);

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default
    )
    {
        CaptureAuditEntries();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void CaptureAuditEntries()
    {
        if (_capturing || _suppressCaptureDepth > 0)
            return;

        _capturing = true;
        try
        {
            var entries = ChangeTracker
                .Entries()
                .Where(entry => entry.Entity is not AuditEntry)
                .Where(entry => !entry.Metadata.IsOwned())
                .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToList();

            // Owned entities live in their own entries. Replacing an owned object
            // (for example assigning a new Range) can leave the owner Unchanged
            // while the old owned entry is Deleted and the new one is Added.
            // Include the owner so an audit row is still produced for the change.
            var trackedEntities = entries
                .Select(entry => entry.Entity)
                .ToHashSet(ReferenceEqualityComparer.Instance);

            var ownersWithOwnedChanges = ChangeTracker
                .Entries()
                .Where(entry => entry.Metadata.IsOwned())
                .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Select(FindRootOwner)
                .Where(entry => entry is not null)
                .Select(entry => entry!)
                .DistinctBy(entry => entry.Entity, ReferenceEqualityComparer.Instance)
                .Where(entry => !trackedEntities.Contains(entry.Entity))
                .ToList();

            entries.AddRange(ownersWithOwnedChanges);

            if (entries.Count == 0)
                return;

            var auditContext =
                AuditContextScope.Current
                ?? _auditContextAccessor?.GetCurrent()
                ?? AuditContext.System;
            var changeSetId = AuditContextScope.PreferredChangeSetId ?? Guid.CreateVersion7();
            var auditEntries = new List<AuditEntry>(entries.Count);

            // Order non-join entries first so a preferred audit entry ID is assigned to the primary entity row.
            foreach (var entry in entries.OrderBy(e => GetJoinTable(e) is not null))
            {
                var joinTable = GetJoinTable(entry);
                var operation = GetOperation(entry);
                var isJoin = joinTable is not null;

                var entityType = isJoin
                    ? "__join__:" + joinTable
                    : entry.Metadata.ClrType.FullName ?? entry.Metadata.ClrType.Name;

                var changedFields = GetChangedFields(entry);
                var beforeJson = entry.State is EntityState.Modified or EntityState.Deleted or EntityState.Unchanged
                    ? (isJoin
                        ? AuditSnapshot.ToJoinJson(entry, original: true)
                        : AuditSnapshot.ToJson(entry, original: true))
                    : null;

                var afterJson = entry.State is EntityState.Added or EntityState.Modified or EntityState.Unchanged
                    ? (isJoin
                        ? AuditSnapshot.ToJoinJson(entry)
                        : AuditSnapshot.ToJson(entry))
                    : null;

                var entryId = isJoin
                    ? Guid.CreateVersion7()
                    : (AuditContextScope.ConsumePreferredAuditEntryId() ?? Guid.CreateVersion7());

                auditEntries.Add(
                    new AuditEntry
                    {
                        Id = entryId,
                        ChangeSetId = changeSetId,
                        OccurredAt = DateTimeOffset.UtcNow,
                        ActorAccountId = auditContext.ActorAccountId,
                        ActorRole = auditContext.ActorRole,
                        Action = $"{_serviceName}.{(isJoin ? joinTable : entry.Metadata.ClrType.Name)}.{operation}",
                        EntityType = entityType,
                        EntityId = isJoin ? null : GetEntityId(entry),
                        CorrelationId = auditContext.CorrelationId,
                        Operation = operation,
                        BeforeJson = beforeJson,
                        AfterJson = afterJson,
                        ChangedFieldsJson = JsonSerializer.Serialize(changedFields, AuditSnapshot.Options),
                        RequestPath = auditContext.RequestPath,
                        TraceId = auditContext.TraceId,
                    }
                );
            }

            Set<AuditEntry>().AddRange(auditEntries);
        }
        finally
        {
            _capturing = false;
        }
    }

    private static EntityEntry? FindRootOwner(EntityEntry ownedEntry)
    {
        var current = ownedEntry;
        while (current.Metadata.IsOwned())
        {
            var owner = FindOwner(current);
            if (owner is null)
                return null;

            current = owner;
        }

        return current;
    }

    private static EntityEntry? FindOwner(EntityEntry ownedEntry)
    {
        var ownership = ownedEntry.Metadata.FindOwnership();
        if (ownership is null)
            return null;

        var principalType = ownership.PrincipalEntityType;
        foreach (var candidate in ownedEntry.Context.ChangeTracker.Entries())
        {
            if (candidate.Metadata.IsOwned())
                continue;

            if (candidate.Metadata != principalType && candidate.Metadata.ClrType != principalType.ClrType)
                continue;

            if (
                ownership.Properties.Count != ownership.PrincipalKey.Properties.Count
                || ownership.Properties.Count == 0
            )
                continue;

            var matches = true;
            for (var index = 0; index < ownership.Properties.Count; index++)
            {
                var dependent = ownedEntry.Property(ownership.Properties[index].Name);
                var principal = candidate.Property(
                    ownership.PrincipalKey.Properties[index].Name
                );

                var dependentValue = dependent.CurrentValue ?? dependent.OriginalValue;
                var principalValue = principal.CurrentValue ?? principal.OriginalValue;

                if (!Equals(dependentValue, principalValue))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
                return candidate;
        }

        return null;
    }

    private static string GetOperation(EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
            return "Created";

        if (entry.State == EntityState.Deleted)
            return "Deleted";

        var wasDeleted = GetPropertyValue(entry, "IsDeleted", original: true) as bool? ?? false;
        var isDeleted = GetPropertyValue(entry, "IsDeleted", original: false) as bool? ?? false;

        if (wasDeleted != isDeleted)
            return isDeleted ? "SoftDeleted" : "Restored";

        return "Updated";
    }

    private static object? GetPropertyValue(EntityEntry entry, string propertyName, bool original)
    {
        if (entry.Metadata.FindProperty(propertyName) is null)
            return null;

        var property = entry.Property(propertyName);
        return original ? property.OriginalValue : property.CurrentValue;
    }

    private static Guid? GetEntityId(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null || key.Properties.Count != 1)
            return null;

        var value = entry.Property(key.Properties[0].Name).CurrentValue;
        return value is Guid id ? id : null;
    }

    private static IReadOnlyList<string> GetChangedFields(EntityEntry entry)
    {
        var names = entry.State switch
        {
            EntityState.Added => entry.Properties.Select(property => property.Metadata.Name),
            EntityState.Deleted => entry.Properties.Select(property => property.Metadata.Name),
            _ => entry.Properties
                .Where(property => property.IsModified)
                .Select(property => property.Metadata.Name),
        };

        return names.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static string? GetJoinTable(EntityEntry entry)
    {
        var clrType = entry.Metadata.ClrType;
        var isSharedJoinType =
            clrType == typeof(Dictionary<string, object>)
            || (
                clrType.IsGenericType
                && clrType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
            );

        if (!isSharedJoinType)
            return null;

        var table = entry.Metadata.GetTableName();
        if (string.IsNullOrWhiteSpace(table))
            return null;

        var schema = entry.Metadata.GetSchema();
        return string.IsNullOrWhiteSpace(schema) ? table : $"{schema}.{table}";
    }
}
