using Media.Application.Constracts.Data;
using Media.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Media.Infrastructure.Persistence
{
    public sealed class MediaDbContext(DbContextOptions<MediaDbContext> options)
        : DbContext(options),
            IMediaDbContext
    {
        public DbSet<MediaAsset> MediaAssets { get; set; }

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder
                .Entity<MediaAsset>()
                .HasIndex(x => x.FileName);
            modelBuilder.Entity<MediaAsset>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder
                .Entity<MediaAsset>()
                .HasIndex(x => x.ObjectKey)
                .IsUnique();
            modelBuilder.Entity<MediaAsset>().HasIndex(x => x.BucketName);
            modelBuilder.Entity<MediaAsset>().HasIndex(x => x.ContentType);
        }
    }
}
