using Application.Abstracts.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database;

/// <summary>
/// EF Core DbContext for the Media database.
/// </summary>
/// <param name="options">DbContext options (connection string, etc.)</param>
public class MediaDbContext(DbContextOptions<MediaDbContext> options)
    : DbContext(options),
        IMediaDbContext
{
    /// <summary>Set of uploaded media assets persisted in the Media database.</summary>
    public DbSet<MediaAsset> MediaAssets { get; set; }

    /// <summary>
    /// Persists pending changes, first normalizing any <see cref="DateTimeOffset"/> values to
    /// UTC, then delegating to the base implementation.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the save operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default
    )
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

    /// <summary>
    /// Configures the media asset entity: maps it to the <c>media_assets</c> table and applies a
    /// global query filter that excludes soft-deleted records.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MediaAsset>().ToTable("media_assets");
        modelBuilder.Entity<MediaAsset>().HasQueryFilter(x => !x.IsDeleted);
    }
}
