using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Respira.SagaAudit.Application.Abstracts.Data;

namespace Respira.SagaAudit.Infrastructure.Persistence.Database;

/// <summary>
/// EF Core DbContext for the SagaAudit database.
/// </summary>
public class SagaAuditDbContext(DbContextOptions<SagaAuditDbContext> options)
    : DbContext(options), ISagaAuditDbContext
{
    public DbSet<ProcessTracker> ProcessTrackers { get; set; }

    public new async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeDateTimeOffsets();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void NormalizeDateTimeOffsets()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTimeOffset dto && dto.Offset != TimeSpan.Zero)
                {
                    property.CurrentValue = dto.ToUniversalTime();
                }
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProcessTracker>().ToTable("process_trackers");
        modelBuilder.Entity<ProcessTracker>().HasIndex(x => x.SagaId).IsUnique();
        modelBuilder.Entity<ProcessTracker>().HasIndex(x => x.Status);
        modelBuilder.Entity<ProcessTracker>().Property(x => x.Status).HasConversion<string>();
    }
}
