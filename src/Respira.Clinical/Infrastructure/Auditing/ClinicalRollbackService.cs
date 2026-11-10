using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Features.Audit.Rollback;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Auditing;

namespace Respira.Clinical.Infrastructure.Auditing;

/// <summary>
/// Restores Clinical data from a local audit change set. A change set is all
/// rows written by one SaveChanges call, so scalar, owned JSON and many-to-many
/// join-table changes are undone together.
/// </summary>
public sealed class ClinicalRollbackService(
    ClinicalDbContext db,
    IAuditContextAccessor auditContextAccessor,
    ILogger<ClinicalRollbackService> logger
) : IClinicalRollbackService
{
    private static readonly MethodInfo LoadEntityCoreMethod = typeof(ClinicalRollbackService)
        .GetMethod(nameof(LoadEntityCore), BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly ConcurrentDictionary<
        Type,
        Func<ClinicalDbContext, Guid, CancellationToken, Task<object?>>
    > EntityLoaders = new();

    public async Task<ClinicalAuditEntryResult?> GetAsync(
        Guid auditEntryId,
        CancellationToken cancellationToken = default
    )
    {
        var entry = await db
            .AuditEntries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == auditEntryId, cancellationToken);

        return entry is null ? null : Map(entry);
    }

    public async Task<ClinicalRollbackPreview?> PreviewAsync(
        Guid auditEntryId,
        CancellationToken cancellationToken = default
    )
    {
        var entry = await db
            .AuditEntries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == auditEntryId, cancellationToken);

        if (entry is null)
            return null;

        var changeSet = await LoadChangeSetAsync(entry, cancellationToken);
        return await BuildPreviewAsync(entry, changeSet, cancellationToken);
    }

    public async Task<ClinicalRollbackResult?> ApplyAsync(
        Guid auditEntryId,
        CancellationToken cancellationToken = default
    )
    {
        var entry = await db
            .AuditEntries.FirstOrDefaultAsync(x => x.Id == auditEntryId, cancellationToken);

        if (entry is null)
            return null;

        var changeSet = await LoadChangeSetAsync(entry, cancellationToken);
        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(
            async ct =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                using var _ = db.SuppressAuditCapture();

                try
                {
                    var preview = await BuildPreviewAsync(entry, changeSet, ct);
                    if (preview.Conflicts.Count > 0)
                        throw new ClinicalRollbackConflictException(preview);

                    var joins = changeSet.Where(JoinTableRollbackHelper.IsJoinEntry).ToList();
                    var entities = changeSet
                        .Where(item => !JoinTableRollbackHelper.IsJoinEntry(item))
                        .ToList();

                    var joinDeletes = joins.Where(item => item.Operation == "Created").ToList();
                    var joinInserts = joins.Where(item => item.Operation == "Deleted").ToList();
                    var entityDeletes = entities.Where(item => item.Operation == "Created").ToList();
                    var entityRestores = entities.Where(item => item.Operation == "Deleted").ToList();
                    var entityUpdates = entities
                        .Where(item => item.Operation is "Updated" or "SoftDeleted" or "Restored")
                        .ToList();

                    foreach (var join in joinDeletes)
                        await JoinTableRollbackHelper.DeleteRowAsync(db, join, ct);

                    foreach (var item in entityDeletes)
                        await RemoveEntityAsync(item, ct);

                    if (entityDeletes.Count > 0)
                        await db.SaveChangesAsync(ct);

                    foreach (var item in entityUpdates)
                        await ApplyUpdateAsync(item, ct);

                    if (entityUpdates.Count > 0)
                        await db.SaveChangesAsync(ct);

                    foreach (var item in entityRestores)
                        await RestoreEntityAsync(item, ct);

                    if (entityRestores.Count > 0)
                        await db.SaveChangesAsync(ct);

                    foreach (var join in joinInserts)
                        await JoinTableRollbackHelper.InsertRowAsync(db, join, ct);

                    var auditContext = auditContextAccessor.GetCurrent();
                    var rollbackAudit = new AuditEntry
                    {
                        Id = Guid.CreateVersion7(),
                        ChangeSetId = Guid.CreateVersion7(),
                        OccurredAt = DateTimeOffset.UtcNow,
                        ActorAccountId = auditContext.ActorAccountId,
                        ActorRole = auditContext.ActorRole,
                        Action = "Clinical.AuditRollback",
                        EntityType = "AuditRollback",
                        EntityId = entry.Id,
                        CorrelationId = auditContext.CorrelationId,
                        Operation = "Rollback",
                        BeforeJson = JsonSerializer.Serialize(
                            new
                            {
                                rollbackOf = entry.Id,
                                changeSetId = entry.ChangeSetId,
                                changes = preview.Changes.Count,
                            },
                            AuditSnapshot.Options
                        ),
                        ChangedFieldsJson = "[]",
                        RequestPath = auditContext.RequestPath,
                        TraceId = auditContext.TraceId,
                    };

                    db.AuditEntries.Add(rollbackAudit);
                    await db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);

                    logger.LogInformation(
                        "Rolled back Clinical audit entry {AuditEntryId} with {ChangeCount} changes",
                        entry.Id,
                        preview.Changes.Count
                    );

                    return new ClinicalRollbackResult(
                        entry.Id,
                        entry.ChangeSetId,
                        preview.Changes.Count,
                        rollbackAudit.Id,
                        rollbackAudit.OccurredAt
                    );
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    throw;
                }
            },
            cancellationToken
        );
    }

    private async Task<IReadOnlyList<AuditEntry>> LoadChangeSetAsync(
        AuditEntry entry,
        CancellationToken cancellationToken
    )
    {
        if (entry.ChangeSetId == Guid.Empty)
            return [entry];

        return await db
            .AuditEntries.AsNoTracking()
            .Where(item => item.ChangeSetId == entry.ChangeSetId)
            .OrderBy(item => item.OccurredAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<ClinicalRollbackPreview> BuildPreviewAsync(
        AuditEntry root,
        IReadOnlyList<AuditEntry> changeSet,
        CancellationToken cancellationToken
    )
    {
        var changes = new List<ClinicalRollbackChange>();
        var conflicts = new List<ClinicalRollbackConflict>();

        foreach (var entry in changeSet)
        {
            if (entry.Operation == "Rollback")
            {
                conflicts.Add(
                    new ClinicalRollbackConflict(
                        entry.EntityType,
                        entry.EntityId,
                        "A rollback audit entry cannot itself be rolled back"
                    )
                );
                continue;
            }

            if (JoinTableRollbackHelper.IsJoinEntry(entry))
            {
                await PreviewJoinAsync(entry, changes, conflicts, cancellationToken);
                continue;
            }

            var metadata = ResolveEntityType(entry.EntityType);
            if (metadata is null)
            {
                conflicts.Add(
                    new ClinicalRollbackConflict(
                        entry.EntityType,
                        entry.EntityId,
                        "The audited entity type is no longer part of the Clinical model"
                    )
                );
                continue;
            }

            if (entry.EntityId is null)
            {
                conflicts.Add(
                    new ClinicalRollbackConflict(
                        entry.EntityType,
                        null,
                        "The audit entry does not contain a single entity id"
                    )
                );
                continue;
            }

            var entity = await LoadEntityAsync(entry, cancellationToken);
            var currentJson = entity is null
                ? null
                : AuditSnapshot.ToJson(db.Entry(entity));

            if (entry.Operation == "Deleted")
            {
                if (entity is not null)
                {
                    conflicts.Add(
                        new ClinicalRollbackConflict(
                            entry.EntityType,
                            entry.EntityId,
                            "The entity has been recreated after the audited delete"
                        )
                    );
                    continue;
                }

                changes.Add(
                    Change(entry, "Recreate the deleted entity", currentJson, entry.BeforeJson)
                );
                continue;
            }

            if (entity is null)
            {
                conflicts.Add(
                    new ClinicalRollbackConflict(
                        entry.EntityType,
                        entry.EntityId,
                        "The entity no longer exists"
                    )
                );
                continue;
            }

            if (!JsonEquals(currentJson, entry.AfterJson))
            {
                conflicts.Add(
                    new ClinicalRollbackConflict(
                        entry.EntityType,
                        entry.EntityId,
                        "Current data differs from the audited after-state; rollback would overwrite newer changes"
                    )
                );
                continue;
            }

            var description = entry.Operation switch
            {
                "Created" => "Delete the entity created by this change set",
                "SoftDeleted" => "Restore the soft-deleted entity",
                "Restored" => "Soft-delete the entity again",
                _ => "Restore the entity to its audited before-state",
            };

            changes.Add(
                Change(
                    entry,
                    description,
                    currentJson,
                    entry.Operation == "Created" ? null : entry.BeforeJson
                )
            );
        }

        return new ClinicalRollbackPreview(
            root.Id,
            root.ChangeSetId,
            root.Action,
            root.OccurredAt,
            changes,
            conflicts
        );
    }

    private async Task PreviewJoinAsync(
        AuditEntry entry,
        ICollection<ClinicalRollbackChange> changes,
        ICollection<ClinicalRollbackConflict> conflicts,
        CancellationToken cancellationToken
    )
    {
        var exists = await JoinTableRollbackHelper.ExistsAsync(db, entry, cancellationToken);
        var expectedToExist = entry.Operation == "Created";
        if (exists != expectedToExist)
        {
            conflicts.Add(
                new ClinicalRollbackConflict(
                    entry.EntityType,
                    null,
                    exists
                        ? "The audited many-to-many row has been recreated since it was deleted"
                        : "The audited many-to-many row no longer exists; rollback cannot safely confirm its current state"
                )
            );
            return;
        }

        var targetJson = entry.Operation == "Deleted" ? entry.BeforeJson : null;
        var description = entry.Operation == "Created"
            ? "Delete the many-to-many row created by this change set"
            : "Recreate the many-to-many row removed by this change set";

        changes.Add(
            new ClinicalRollbackChange(
                entry.EntityType,
                null,
                entry.Operation,
                description,
                exists ? entry.AfterJson : null,
                targetJson,
                true
            )
        );
    }

    private async Task ApplyUpdateAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entry.BeforeJson))
            throw new InvalidOperationException($"Audit entry {entry.Id} has no before-state");

        var entity = await LoadEntityAsync(entry, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Entity {entry.EntityId} for audit entry {entry.Id} no longer exists"
            );

        var metadata = ResolveEntityType(entry.EntityType)
            ?? throw new InvalidOperationException($"Cannot resolve entity type {entry.EntityType}");

        var ownedNavigations = metadata
            .GetNavigations()
            .Where(navigation => navigation.TargetEntityType.IsOwned())
            .Select(navigation => navigation.Name)
            .ToHashSet(StringComparer.Ordinal);

        AuditSnapshot.Apply(entity, entry.BeforeJson, null, ownedNavigations);

        var entityEntry = db.Entry(entity);
        foreach (var property in entityEntry.Properties)
        {
            if (property.Metadata.IsPrimaryKey() || property.Metadata.IsShadowProperty())
                continue;

            if (property.Metadata.ValueGenerated == ValueGenerated.OnAddOrUpdate)
                continue;

            property.IsModified = true;
        }

        foreach (var navigation in entityEntry.Navigations)
        {
            if (!navigation.Metadata.TargetEntityType.IsOwned())
                continue;

            if (navigation is ReferenceEntry { CurrentValue: not null, TargetEntry: { } target })
                target.State = EntityState.Modified;
            else if (navigation is CollectionEntry collection)
                foreach (var ownedTarget in collection.CurrentValue?.Cast<object>() ?? [])
                    db.Entry(ownedTarget).State = EntityState.Modified;
        }
    }

    private Task RestoreEntityAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entry.BeforeJson))
            throw new InvalidOperationException($"Audit entry {entry.Id} has no before-state");

        var metadata = ResolveEntityType(entry.EntityType)
            ?? throw new InvalidOperationException($"Cannot resolve entity type {entry.EntityType}");

        var entity = Activator.CreateInstance(metadata.ClrType)
            ?? throw new InvalidOperationException($"Cannot create entity {entry.EntityType}");

        AuditSnapshot.Apply(entity, entry.BeforeJson);
        db.Add(entity);
        return Task.CompletedTask;
    }

    private async Task RemoveEntityAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        var entity = await LoadEntityAsync(entry, cancellationToken);
        if (entity is not null)
            db.Remove(entity);
    }

    private Task<object?> LoadEntityAsync(
        AuditEntry entry,
        CancellationToken cancellationToken
    )
    {
        if (entry.EntityId is null)
            return Task.FromResult<object?>(null);

        var metadata = ResolveEntityType(entry.EntityType);
        if (metadata is null)
            return Task.FromResult<object?>(null);

        var loader = EntityLoaders.GetOrAdd(
            metadata.ClrType,
            static clrType =>
                LoadEntityCoreMethod
                    .MakeGenericMethod(clrType)
                    .CreateDelegate<Func<ClinicalDbContext, Guid, CancellationToken, Task<object?>>>()
        );

        return loader(db, entry.EntityId.Value, cancellationToken);
    }

    private static async Task<object?> LoadEntityCore<TEntity>(
        ClinicalDbContext context,
        Guid id,
        CancellationToken cancellationToken
    )
        where TEntity : class
    {
        return await context
            .Set<TEntity>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(entity => EF.Property<Guid>(entity, "Id") == id, cancellationToken);
    }

    private IEntityType? ResolveEntityType(string entityType) =>
        db.Model
            .GetEntityTypes()
            .FirstOrDefault(type =>
                type.ClrType.FullName == entityType || type.ClrType.Name == entityType
            );

    private static ClinicalRollbackChange Change(
        AuditEntry entry,
        string description,
        string? currentJson,
        string? targetJson
    ) =>
        new(
            entry.EntityType,
            entry.EntityId,
            entry.Operation,
            description,
            currentJson,
            targetJson,
            false
        );

    private static bool JsonEquals(string? left, string? right)
    {
        if (left is null || right is null)
            return left is null && right is null;

        var leftNode = JsonNode.Parse(left);
        var rightNode = JsonNode.Parse(right);
        RemoveReferenceIds(leftNode);
        RemoveReferenceIds(rightNode);
        return JsonNode.DeepEquals(leftNode, rightNode);
    }

    private static void RemoveReferenceIds(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                jsonObject.Remove("$refs");
                foreach (var key in jsonObject.Select(pair => pair.Key).ToList())
                    RemoveReferenceIds(jsonObject[key]);
                break;
            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                    RemoveReferenceIds(item);
                break;
        }
    }

    private static ClinicalAuditEntryResult Map(AuditEntry entry) =>
        new(
            entry.Id,
            entry.ChangeSetId,
            entry.OccurredAt,
            entry.ActorAccountId,
            entry.ActorRole,
            entry.Action,
            entry.EntityType,
            entry.EntityId,
            entry.CorrelationId,
            entry.Operation,
            entry.BeforeJson,
            entry.AfterJson,
            entry.ChangedFieldsJson,
            entry.RequestPath,
            entry.TraceId,
            entry.ForwardedAt
        );
}
