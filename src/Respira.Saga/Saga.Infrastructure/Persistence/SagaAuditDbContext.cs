using Microsoft.EntityFrameworkCore;
using Respira.Saga.Application.Constracts.Data;
using Respira.Saga.Domain.Entities;

namespace Respira.Saga.Infrastructure.Persistence;

public sealed class SagaAuditDbContext(DbContextOptions<SagaAuditDbContext> options)
    : DbContext(options), ISagaAuditDbContext
{
    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AuditLogEntry>();
        entity.ToTable("AuditLogs");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).ValueGeneratedNever();
        entity.Property(item => item.Action).HasMaxLength(128).IsRequired();
        entity.Property(item => item.ActorRole).HasMaxLength(64).IsRequired();
        entity.Property(item => item.EntityType).HasMaxLength(128).IsRequired();
        entity.Property(item => item.DetailsJson).HasColumnType("jsonb").IsRequired();
        entity.HasIndex(item => item.OccurredAt);
        entity.HasIndex(item => new { item.EntityType, item.EntityId });
        entity.HasIndex(item => item.ActorAccountId);
    }
}
