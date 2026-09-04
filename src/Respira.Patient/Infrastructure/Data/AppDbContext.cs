using Application.Contracts.Data;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IDbContext
{
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Treatment> Treatments { get; set; }

    public async override ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
    }

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
        modelBuilder.Entity<Patient>().Property(x => x.Status).HasConversion<string>();

        // Configure on Treatment
        modelBuilder.Entity<Treatment>()
            .UseTphMappingStrategy()
            .ToTable("treatments")
            .Ignore(x => x.TreatmentType)
            .HasDiscriminator<string>("type")
            .HasValue<EmpiricalTreatment>("empirical_treatment")
            .HasValue<TargetedTreatment>("targeted_treatment");
        // modelBuilder.Entity<Treatment>()
        //     .OwnsOne(x => x.DiagnosisRecord, builder => builder.ToJson());
        // modelBuilder.Entity<EmpiricalTreatment>()
        //     .OwnsOne(x => x.EmpiricalDiagnosisRecord, builder => builder.ToJson());
        // modelBuilder.Entity<TargetedTreatment>()
        //     .OwnsOne(x => x.TargetedDiagnosisRecord, builder => builder.ToJson());

        modelBuilder.Entity<Treatment>()
            .Ignore(x => x.DiagnosisRecord);
        modelBuilder.Entity<EmpiricalTreatment>()
            .Property(x => x.Status)
            .HasConversion<string>();
        modelBuilder.Entity<EmpiricalTreatment>()
            .OwnsOne(x => x.EmpiricalDiagnosisRecord, owned =>
            {
                owned.ToJson();
                owned.OwnsMany(d => d.SystemRecommendedMedicines);
                owned.OwnsMany(d => d.DoctorChosenMedicines);
                owned.OwnsMany(d => d.InfectionProbabilityRecords, ip => ip.OwnsOne(x => x.Pathogen));
            });
        modelBuilder.Entity<TargetedTreatment>()
            .OwnsOne(x => x.TargetedDiagnosisRecord, owned =>
            {
                owned.ToJson();
                owned.OwnsMany(d => d.SystemRecommendedMedicines);
                owned.OwnsMany(d => d.DoctorChosenMedicines);
                owned.OwnsOne(d => d.Pathogen);
            });
    }
}
