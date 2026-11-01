using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

using Respira.Saga.Domain.Entities;
using Respira.Saga.Infrastructure.Persistence;

namespace Respira.Saga.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SagaAuditDbContext))]
public sealed class SagaAuditDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.12");
        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedNever();
            entity.Property(item => item.OccurredAt).IsRequired();
            entity.Property(item => item.ActorAccountId);
            entity.Property(item => item.Action).HasMaxLength(128).IsRequired();
            entity.Property(item => item.ActorRole).HasMaxLength(64).IsRequired();
            entity.Property(item => item.EntityType).HasMaxLength(128).IsRequired();
            entity.Property(item => item.EntityId);
            entity.Property(item => item.CorrelationId);
            entity.Property(item => item.DetailsJson).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(item => item.OccurredAt);
            entity.HasIndex(item => new { item.EntityType, item.EntityId });
            entity.HasIndex(item => item.ActorAccountId);
        });
    }
}
