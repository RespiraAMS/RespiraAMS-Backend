using System.Text.Json;
using Application.Abstracts.Data;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database
{
    /// <summary>
    /// EF Core DbContext for the Doctor database
    /// </summary>
    /// <param name="options">DbContext options (connection string, etc.)</param>
    public class DoctorDbContext(DbContextOptions<DoctorDbContext> options)
        : DbContext(options),
            IDoctorDbContext
    {
        /// <summary>
        /// Registered doctors
        /// </summary>
        public DbSet<Doctor> Doctors { get; set; }

        /// <summary>
        /// Saves changes without a cancellation token (required by <see cref="IDoctorDbContext"/>)
        /// </summary>
        /// <returns>Number of affected rows</returns>
        public Task<int> SaveChangesAsync() => base.SaveChangesAsync();

        /// <summary>
        /// Configures entity mappings: table names, indexes, soft-delete filters, enum conversions
        /// </summary>
        /// <param name="modelBuilder">Model builder</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Doctor>().ToTable("doctors");
            modelBuilder.Entity<Doctor>().HasIndex(x => x.CitizenIdentificationNumber).IsUnique();
            modelBuilder.Entity<Doctor>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Doctor>().Property(x => x.AcademicTitle).HasConversion<string>();
            modelBuilder.Entity<Doctor>().Property(x => x.Position).HasConversion<string>();
            modelBuilder.Entity<Doctor>().Property(x => x.Gender).HasConversion<string>();

            modelBuilder
                .Entity<Doctor>()
                .Property(x => x.Degrees)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v =>
                        JsonSerializer.Deserialize<List<DegreeType>>(
                            v,
                            (JsonSerializerOptions?)null
                        ) ?? new List<DegreeType>()
                );
            modelBuilder
                .Entity<Doctor>()
                .Property(x => x.Patients)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null)
                );

            modelBuilder
                .Entity<Doctor>()
                .HasMany(x => x.Subordinates)
                .WithOne()
                .HasForeignKey("ManagerId")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
