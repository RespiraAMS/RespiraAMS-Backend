using Application.Contracts.Data;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Respira.ServiceDefaults.Models;

namespace Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IDbContext
{
    private IExecutionStrategy GetExecutionStrategy() => base.Database.CreateExecutionStrategy();

    public DbSet<Antibiogram> Antibiograms { get; set; }
    public DbSet<Antibiotic> Antibiotics { get; set; }
    public DbSet<AntibioticGroup> AntibioticGroups { get; set; }
    public DbSet<Cause> Causes { get; set; }
    public DbSet<Criterion> Criteria { get; set; }
    public DbSet<Disease> Diseases { get; set; }
    public DbSet<Dosage> Dosages { get; set; }
    public DbSet<EmpiricTreatmentProtocol> EmpiricTreatmentProtocols { get; set; }
    public DbSet<IcuHospitalizeCriterion> IcuHospitalizeCriteria { get; set; }
    public DbSet<Pathogen> Pathogens { get; set; }
    public DbSet<ResistanceRiskFactor> ResistanceRiskFactors { get; set; }

    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        var strategy = GetExecutionStrategy();
        await strategy.ExecuteAsync(
            action,
            async (ctx, op, token) =>
            {
                await using var transaction = await ctx.Database
                    .BeginTransactionAsync(token);

                try
                {
                    await op();
                    await ctx.SaveChangesAsync(token);
                    await transaction.CommitAsync(token);
                }
                catch
                {
                    await transaction.RollbackAsync(token);
                    throw;
                }

                return true;
            },
            null,
            cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(Action action, CancellationToken cancellationToken = default)
    {
        var strategy = GetExecutionStrategy();
        await strategy.ExecuteAsync(
            action,
            async (ctx, op, token) =>
            {
                await using var transaction = await ctx.Database
                    .BeginTransactionAsync(token);

                try
                {
                    op();
                    await ctx.SaveChangesAsync(token);
                    await transaction.CommitAsync(token);
                }
                catch
                {
                    await transaction.RollbackAsync(token);
                    throw;
                }

                return true;
            },
            null,
            cancellationToken);
    }

    public T AttachStub<T>(Guid id) where T : Base
    {
        // If the entity has been tracked by EF Core
        var tracked = Set<T>().Local.FirstOrDefault(x => x.Id == id);
        if (tracked is not null)
        {
            return tracked;
        }

        // Create a stub object (fake object that just has the ID) -> save memory
        if (typeof(T) == typeof(Criterion))
        {
            // Criterion is abstract, so we instantiate a concrete subclass (e.g., BooleanCriterion)
            var criterionStub = new BooleanCriterion { Name = string.Empty };

            // Attach to database set as Unchanged
            Set<Criterion>().Attach(criterionStub);

            return (T)(object)criterionStub;
        }

        var stub = Activator.CreateInstance<T>();
        stub.Id = id;
        Set<T>().Attach(stub);
        return stub;
    }

    public void UpdateRelations<T>(ICollection<T> collection, IEnumerable<Guid>? ids) where T : Base
    {
        if (ids == null) return;
        var newIds = ids.ToHashSet();

        // Remove items no longer in the list
        var toRemove = collection.Where(x => !newIds.Contains(x.Id)).ToList();
        foreach (var item in toRemove)
        {
            collection.Remove(item);
        }

        // Add new items only
        var existingIds = collection.Select(x => x.Id).ToHashSet();
        foreach (var id in newIds.Where(id => !existingIds.Contains(id)))
        {
            collection.Add(AttachStub<T>(id));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Setup global query filter: filter soft delete items
        modelBuilder.Entity<Antibiogram>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Antibiotic>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<AntibioticGroup>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Cause>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Criterion>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Disease>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Dosage>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<EmpiricTreatmentProtocol>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<IcuHospitalizeCriterion>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Pathogen>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ResistanceRiskFactor>().HasQueryFilter(x => !x.IsDeleted);

        // Config on criterion
        modelBuilder.Entity<Criterion>()
            .UseTphMappingStrategy()
            .Ignore(x => x.Type)
            .ToTable("criteria")
            .HasDiscriminator<string>("type")
            .HasValue<BooleanCriterion>("boolean")
            .HasValue<NumericCriterion>("numeric");
        modelBuilder.Entity<NumericCriterion>()
            .OwnsOne(x => x.Value, builder => builder.ToJson());
        modelBuilder.Entity<NumericCriterion>()
            .HasIndex(x => x.Value)
            .HasMethod("gin"); // Use GIN index for better performance with JSONB

        // Config on antibiotic group
        modelBuilder.Entity<AntibioticGroup>().ToTable("antibiotic_groups");
        modelBuilder.Entity<AntibioticGroup>()
            .HasOne(x => x.Parent)
            .WithMany()
            .HasForeignKey(x => x.ParentId);
        modelBuilder.Entity<AntibioticGroup>()
            .HasIndex(x => x.Name);

        // Config on dosage
        modelBuilder.Entity<Dosage>().ToTable("dosages");
        modelBuilder.Entity<Dosage>()
            .HasOne(x => x.Antibiotic)
            .WithMany(x => x.Dosages)
            .HasForeignKey(x => x.AntibioticId);
        modelBuilder.Entity<Dosage>()
            .Property(x => x.RouteOfAdministration)
            .HasConversion<string>();
        modelBuilder.Entity<Dosage>()
            .OwnsOne(x => x.GlomerularFiltrationRate, builder => builder.ToJson());
        modelBuilder.Entity<Dosage>()
            .HasIndex(x => x.GlomerularFiltrationRate)
            .HasMethod("gin"); // Use GIN index for better performance with JSONB

        // Config on antibiotic
        modelBuilder.Entity<Antibiotic>().ToTable("antibiotics");
        modelBuilder.Entity<Antibiotic>()
            .HasOne(x => x.AntibioticGroup)
            .WithMany()
            .HasForeignKey(x => x.AntibioticGroupId);
        modelBuilder.Entity<Antibiotic>()
            .HasIndex(x => new { x.Name, x.Category });
        modelBuilder.Entity<Antibiotic>()
            .Property(x => x.Category)
            .HasConversion<string>();

        // Config on pathogen
        modelBuilder.Entity<Pathogen>().ToTable("pathogens");
        modelBuilder.Entity<Pathogen>()
            .HasMany(x => x.Medicines)
            .WithMany(x => x.AntibioticSpectra)
            .UsingEntity(t => t.ToTable("antibiotic_pathogen"));

        // Config on disease
        modelBuilder.Entity<Disease>().ToTable("diseases");
        modelBuilder.Entity<Disease>().HasIndex(x => x.Name);

        // Config on disease's causes
        modelBuilder.Entity<Cause>().ToTable("disease_causes");
        modelBuilder.Entity<Cause>()
            .HasOne(x => x.Disease)
            .WithMany(x => x.Causes)
            .HasForeignKey(x => x.DiseaseId);
        modelBuilder.Entity<Cause>()
            .HasOne(x => x.Pathogen)
            .WithMany()
            .HasForeignKey(x => x.PathogenId);
        modelBuilder.Entity<Cause>()
            .Property(x => x.Severity)
            .HasConversion<string>();
        modelBuilder.Entity<Cause>()
            .Property(x => x.TreatmentSite)
            .HasConversion<string>();

        // Config on ICU hospitalize criteria
        modelBuilder.Entity<IcuHospitalizeCriterion>().ToTable("icu_hospitalize_criteria");
        modelBuilder.Entity<IcuHospitalizeCriterion>()
            .HasOne(x => x.Disease)
            .WithMany(x => x.IcuHospitalizeCriteria)
            .HasForeignKey(x => x.DiseaseId);

        // Config on resistance risk factor
        modelBuilder.Entity<ResistanceRiskFactor>().ToTable("resistance_risk_factors");
        modelBuilder.Entity<ResistanceRiskFactor>()
            .HasOne(x => x.Disease)
            .WithMany(x => x.ResistanceRiskFactors)
            .HasForeignKey(x => x.DiseaseId);
        modelBuilder.Entity<ResistanceRiskFactor>()
            .HasOne(x => x.Pathogen)
            .WithMany()
            .HasForeignKey(x => x.PathogenId);

        // Config on empiric treatment protocol
        modelBuilder.Entity<EmpiricTreatmentProtocol>().ToTable("empiric_treatment_protocols");
        modelBuilder.Entity<EmpiricTreatmentProtocol>()
            .HasOne(x => x.Disease)
            .WithMany(x => x.EmpiricTreatmentProtocols)
            .HasForeignKey(x => x.DiseaseId);
        modelBuilder.Entity<EmpiricTreatmentProtocol>()
            .HasOne(x => x.SpecialInfection)
            .WithMany()
            .HasForeignKey(x => x.SpecialInfectionId);
        modelBuilder.Entity<EmpiricTreatmentProtocol>()
            .HasMany(x => x.OtherCriteria)
            .WithMany()
            .UsingEntity(t => t.ToTable("empiric_treatment_protocol_other_criteria"));
        modelBuilder.Entity<EmpiricTreatmentProtocol>()
            .HasMany(x => x.Medicines)
            .WithMany()
            .UsingEntity(t => t.ToTable("empiric_treatment_protocol_medicine"));
        modelBuilder.Entity<EmpiricTreatmentProtocol>()
            .Property(x => x.Severity)
            .HasConversion<string>();
        modelBuilder.Entity<EmpiricTreatmentProtocol>()
            .Property(x => x.TreatmentSite)
            .HasConversion<string>();

        // Config on antibiogram
        modelBuilder.Entity<Antibiogram>().ToTable("antibiograms");
        modelBuilder.Entity<Antibiogram>()
            .HasOne<Pathogen>()
            .WithMany()
            .HasForeignKey(x => x.PathogenId);
        modelBuilder.Entity<Antibiogram>()
            .HasIndex(x => x.PathogenId);
        modelBuilder.Entity<Antibiogram>()
            .HasMany(x => x.Mics)
            .WithMany()
            .UsingEntity(t => t.ToTable("antibiogram_mic_groups"));
        modelBuilder.Entity<Antibiogram>()
            .HasMany(x => x.FirstPriorityMedicines)
            .WithMany()
            .UsingEntity(t => t.ToTable("antibiogram_first_priority_medicines"));
        modelBuilder.Entity<Antibiogram>()
            .HasMany(x => x.SecondPriorityMedicines)
            .WithMany()
            .UsingEntity(t => t.ToTable("antibiogram_second_priority_medicines"));
        modelBuilder.Entity<Antibiogram>()
            .Property(x => x.MicLevel)
            .HasConversion<string>();
    }
}