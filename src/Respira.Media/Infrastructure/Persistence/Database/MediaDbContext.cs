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
    public DbSet<MediaAsset> MediaAssets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MediaAsset>().ToTable("media_assets");
        modelBuilder.Entity<MediaAsset>().HasQueryFilter(x => !x.IsDeleted);
    }
}
