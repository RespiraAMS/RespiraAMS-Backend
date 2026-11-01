using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Infrastructure.Util.Database;
using Respira.ServiceDefaults.Models;
using Respira.ServiceDefaults.Utils.FullTextSearch;

namespace Respira.Clinical.Infrastructure.Data
{
    public class ClinicalDbContext(DbContextOptions<ClinicalDbContext> options) : DbContext(options), IDbContext
    {
        private IExecutionStrategy GetExecutionStrategy() => base.Database.CreateExecutionStrategy();

        public DbSet<ClinicalVariable> ClinicalVariables { get; set; }
        public DbSet<Criterion> Criteria { get; set; }
        public DbSet<Pathogen> Pathogens { get; set; }
        public DbSet<RiskFactor> RiskFactors { get; set; }
        public DbSet<ClinicalMetric> ClinicalMetrics { get; set; }
        public DbSet<MetricRule> MetricRules { get; set; }
        public DbSet<SuspectedCause> SuspectedCauses { get; set; }
        public DbSet<Antibiotic> Antibiotics { get; set; }
        public DbSet<AntibioticGroup> AntibioticGroups { get; set; }
        public DbSet<Dosage> Dosages { get; set; }
        public DbSet<Treatment> Treatments { get; set; }
        public DbSet<TreatmentProtocol> TreatmentProtocols { get; set; }
        public DbSet<Antibiogram> Antibiograms { get; set; }

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
            foreach (var item in collection.Where(x => !newIds.Contains(x.Id)).ToList())
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

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply query filter to soft-delete items
            modelBuilder.Entity<ClinicalVariable>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Criterion>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Pathogen>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<RiskFactor>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<ClinicalMetric>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<MetricRule>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<SuspectedCause>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Treatment>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<TreatmentProtocol>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Antibiotic>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<AntibioticGroup>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Dosage>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Antibiogram>().HasQueryFilter(x => !x.IsDeleted);

            // Activate unaccent extension for full text search on Vietnamese dataset
            modelBuilder.HasPostgresExtension("unaccent");

            // Config on clinical variable
            modelBuilder.Entity<ClinicalVariable>()
                .UseTphMappingStrategy()
                .ToTable("clinical_variables")
                .Ignore(x => x.ValueType)
                .HasDiscriminator<string>("value_type")
                .HasValue<BooleanClinicalVariable>("boolean_clinical_variable")
                .HasValue<NumericClinicalVariable>("numeric_clinical_variable")
                .HasValue<CategoricalClinicalVariable>("categorical_clinical_variable");
            modelBuilder.Entity<ClinicalVariable>()
                .Property(x => x.Prerequisite)
                .HasFormulaConversion();
            modelBuilder.Entity<ClinicalVariable>()
                .Property(x => x.Category)
                .HasConversion<string>();
            modelBuilder.Entity<NumericClinicalVariable>()
                .OwnsOne(x => x.AcceptedRange, builder => builder.ToJson());
            modelBuilder.Entity<NumericClinicalVariable>()
                .OwnsOne(x => x.NormalRange, builder => builder.ToJson());
            modelBuilder.Entity<ClinicalVariable>()
                .HasGeneratedTsVectorColumn(
                    x => x.SearchVector,
                    FullTextSearchUtil.ConfigurationName,
                    x => new { x.Name, x.Code, x.Description })
                .HasIndex(x => x.SearchVector)
                .HasMethod("GIN");

            modelBuilder.Entity<Criterion>().ToTable("criteria");
            modelBuilder.Entity<Criterion>().Ignore(x => x.Variables);
            modelBuilder.Entity<Criterion>()
                .Property(x => x.Formula)
                .HasFormulaConversion();

            // Config on pathogen
            modelBuilder.Entity<Pathogen>().ToTable("pathogens");
            modelBuilder.Entity<Pathogen>()
                .HasGeneratedTsVectorColumn(
                    x => x.SearchVector,
                    FullTextSearchUtil.ConfigurationName,
                    x => new { x.Name, x.Description })
                .HasIndex(x => x.SearchVector)
                .HasMethod("GIN");

            // Config on risk factor
            modelBuilder.Entity<RiskFactor>().ToTable("risk_factors");
            modelBuilder.Entity<RiskFactor>()
                .HasOne(x => x.Pathogen)
                .WithMany(x => x.RiskFactors)
                .HasForeignKey(x => x.PathogenId);
            modelBuilder.Entity<RiskFactor>()
                .HasOne(x => x.Criterion)
                .WithMany()
                .HasForeignKey(x => x.CriterionId);
            modelBuilder.Entity<RiskFactor>().Ignore(x => x.Variables);

            // Config on clinical metrics
            modelBuilder.Entity<ClinicalMetric>().ToTable("clinical_metrics");
            modelBuilder.Entity<ClinicalMetric>()
                .HasGeneratedTsVectorColumn(
                    x => x.SearchVector,
                    FullTextSearchUtil.ConfigurationName,
                    x => new { x.Name, x.Code, x.Description })
                .HasIndex(x => x.SearchVector)
                .HasMethod("GIN");

            // Config on suspected cause
            modelBuilder.Entity<SuspectedCause>().ToTable("suspected_causes");
            modelBuilder.Entity<SuspectedCause>()
                .Property(x => x.Severity)
                .HasConversion<string>();
            modelBuilder.Entity<SuspectedCause>()
                .Property(x => x.TreatmentSite)
                .HasConversion<string>();

            // Config on metrics rule (scoring / major-minor)
            modelBuilder.Entity<MetricRule>()
                .UseTphMappingStrategy()
                .ToTable("metric_rules")
                .HasDiscriminator<string>("rule_type")
                .HasValue<ScoringRule>("scoring_rule")
                .HasValue<MajorMinorRule>("major_minor_rule");
            modelBuilder.Entity<MetricRule>()
                .HasOne(x => x.ClinicalMetric)
                .WithMany(x => x.Rules)
                .HasForeignKey(x => x.ClinicalMetricId);
            modelBuilder.Entity<MetricRule>()
                .HasOne(x => x.Criterion)
                .WithMany()
                .HasForeignKey(x => x.CriterionId);
            modelBuilder.Entity<ScoringRule>().Ignore(x => x.Variables);
            modelBuilder.Entity<ScoringRule>().Property(x => x.ScoreFunction).HasFormulaConversion();
            modelBuilder.Entity<MajorMinorRule>().Ignore(x => x.Variables);

            // Config on antibiotic group
            modelBuilder.Entity<AntibioticGroup>().ToTable("antibiotic_groups");
            modelBuilder.Entity<AntibioticGroup>()
                .HasOne(x => x.Parent)
                .WithMany()
                .HasForeignKey(x => x.ParentId);
            modelBuilder.Entity<AntibioticGroup>()
                .HasGeneratedTsVectorColumn(
                    x => x.SearchVector,
                    FullTextSearchUtil.ConfigurationName,
                    x => new { x.Name, x.Description })
                .HasIndex(x => x.SearchVector)
                .HasMethod("GIN");

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
                .OwnsOne(x => x.Crcl, builder => builder.ToJson());
            modelBuilder.Entity<Dosage>().Ignore(x => x.IsStandardDose);

            // Config on antibiotic
            modelBuilder.Entity<Antibiotic>().ToTable("antibiotics");
            modelBuilder.Entity<Antibiotic>()
                .HasOne(x => x.AntibioticGroup)
                .WithMany()
                .HasForeignKey(x => x.AntibioticGroupId);
            modelBuilder.Entity<Antibiotic>()
                .Property(x => x.Classification)
                .HasConversion<string>();
            modelBuilder.Entity<Antibiotic>()
                .HasGeneratedTsVectorColumn(
                    x => x.SearchVector,
                    FullTextSearchUtil.ConfigurationName,
                    x => new { x.Name })
                .HasIndex(x => x.SearchVector)
                .HasMethod("GIN");

            // Config on medicine composition
            modelBuilder.Entity<Treatment>().ToTable("treatments");
            modelBuilder.Entity<Treatment>()
                .HasMany(x => x.Combination)
                .WithMany()
                .UsingEntity(t => t.ToTable("treatment_antibiotics"));
            modelBuilder.Entity<Treatment>()
                .HasOne(x => x.TreatmentProtocol)
                .WithMany(x => x.Treatments)
                .HasForeignKey(x => x.TreatmentProtocolId);

            // Config on treatment
            modelBuilder.Entity<TreatmentProtocol>().ToTable("treatment_protocols");
            modelBuilder.Entity<TreatmentProtocol>()
                .HasMany(x => x.Pathogens)
                .WithMany()
                .UsingEntity(t => t.ToTable("treatment_protocol_pathogens"));
            modelBuilder.Entity<TreatmentProtocol>()
                .HasMany(x => x.Criteria)
                .WithMany()
                .UsingEntity(t => t.ToTable("treatment_protocol_criteria"));
            modelBuilder.Entity<TreatmentProtocol>()
                .Property(x => x.Severity)
                .HasConversion<string>();
            modelBuilder.Entity<TreatmentProtocol>()
                .Property(x => x.TreatmentSite)
                .HasConversion<string>();

            modelBuilder.Entity<Antibiogram>().ToTable("antibiograms");
            modelBuilder.Entity<Antibiogram>()
                .HasOne(x => x.Pathogen)
                .WithMany()
                .HasForeignKey(x => x.PathogenId);
            modelBuilder.Entity<Antibiogram>()
                .HasMany(x => x.Antibiotics)
                .WithMany()
                .UsingEntity(t => t.ToTable("antibiogram_antibiotics"));
            modelBuilder.Entity<Antibiogram>().Property(x => x.MicCategory).HasConversion<string>();
        }

        public override async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            await base.DisposeAsync();
        }
    }
}
