using Application.Contracts.Data;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IDbContext
{
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Treatment> Treatments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply global query filter
        modelBuilder.Entity<Patient>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Treatment>().HasQueryFilter(x => !x.IsDeleted);

        // Configure on Patient
        modelBuilder.Entity<Patient>().ToTable("patients");
        modelBuilder.Entity<Patient>()
            .HasMany(x => x.Treatments)
            .WithOne(x => x.Patient);
        modelBuilder.Entity<Patient>().HasIndex(x => x.FullName);
        modelBuilder.Entity<Patient>().HasIndex(x => x.MedicalRecordCode);

        // Configure on Treatment
        modelBuilder.Entity<Treatment>()
            .UseTphMappingStrategy()
            .ToTable("treatments")
            .Ignore(x => x.TreatmentType)
            .HasDiscriminator<string>("type")
            .HasValue<EmpiricalTreatment>("empirical_treatment")
            .HasValue<TargetedTreatment>("targeted_treatment");
        modelBuilder.Entity<Treatment>()
            .OwnsMany(x => x.MedicineRecords, builder => builder.ToJson());
        modelBuilder.Entity<EmpiricalTreatment>()
            .OwnsMany(x => x.InfectionProbabilityRecords, builder => builder.ToJson());
    }
}