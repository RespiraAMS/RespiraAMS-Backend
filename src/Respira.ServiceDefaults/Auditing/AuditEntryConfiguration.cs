using Microsoft.EntityFrameworkCore;

namespace Respira.ServiceDefaults.Auditing;

public static class AuditEntryConfiguration
{
    /// <summary>
    /// Adds the local <c>audit_entries</c> table to a service DbContext model.
    /// </summary>
    public static void ConfigureAuditEntries(this ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AuditEntry>();

        entity.ToTable("audit_entries");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.ChangeSetId).IsRequired();
        entity.Property(x => x.OccurredAt).IsRequired();
        entity.Property(x => x.ActorRole).HasMaxLength(128).IsRequired();
        entity.Property(x => x.Action).HasMaxLength(256).IsRequired();
        entity.Property(x => x.EntityType).HasMaxLength(256).IsRequired();
        entity.Property(x => x.Operation).HasMaxLength(64).IsRequired();
        entity.Property(x => x.BeforeJson).HasColumnType("jsonb");
        entity.Property(x => x.AfterJson).HasColumnType("jsonb");
        entity.Property(x => x.ChangedFieldsJson).HasColumnType("jsonb").IsRequired();
        entity.Property(x => x.RequestPath).HasMaxLength(512);
        entity.Property(x => x.TraceId).HasMaxLength(128);

        entity.HasIndex(x => new { x.ForwardedAt, x.OccurredAt });
        entity.HasIndex(x => x.ChangeSetId);
        entity.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAt });
        entity.HasIndex(x => x.Action);
    }
}
