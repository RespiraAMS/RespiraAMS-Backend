using Microsoft.EntityFrameworkCore;
using Respira.Doctor.Application.Constracts.Data;
using Respira.Doctor.Domain.Entities;
using DoctorEntity = Respira.Doctor.Domain.Entities.Doctor;

namespace Respira.Doctor.Infrastructure.Persistence
{
    public sealed class DoctorDbContext(DbContextOptions<DoctorDbContext> options)
        : DbContext(options),
            IDoctorDbContext
    {
        public DbSet<DoctorEntity> Doctors { get; set; }
        public DbSet<Manager> Managers { get; set; }

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

            modelBuilder.Entity<DoctorEntity>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<DoctorEntity>().HasIndex(x => x.AccountId).IsUnique();
            modelBuilder.Entity<DoctorEntity>().HasIndex(x => x.MediaId);
            modelBuilder.Entity<DoctorEntity>().HasIndex(x => x.CitizenIdentificationNumber);
            modelBuilder.Entity<DoctorEntity>().Property(x => x.Position).HasConversion<string>();
            modelBuilder
                .Entity<DoctorEntity>()
                .Property(x => x.AcademicTitle)
                .HasConversion<string>();
            modelBuilder.Entity<DoctorEntity>().Property(x => x.Gender).HasConversion<string>();

            modelBuilder.Entity<Manager>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Manager>().HasIndex(x => x.AccountId).IsUnique();
            modelBuilder.Entity<Manager>().HasIndex(x => x.MediaId);
            modelBuilder.Entity<Manager>().HasIndex(x => x.CitizenIdentificationNumber);
            modelBuilder.Entity<Manager>().Property(x => x.Position).HasConversion<string>();
            modelBuilder.Entity<Manager>().Property(x => x.AcademicTitle).HasConversion<string>();
            modelBuilder.Entity<Manager>().Property(x => x.Gender).HasConversion<string>();
        }
    }
}
