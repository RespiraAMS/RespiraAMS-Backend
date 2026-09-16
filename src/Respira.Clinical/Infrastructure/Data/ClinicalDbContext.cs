using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Respira.Application.Contracts.Data;
using Respira.Domain.Entities;
using Respira.Domain.Models;
using Respira.Infrastructure.Util.Database;
using Respira.ServiceDefaults.Models;

namespace Respira.Infrastructure.Data
{
    public class ClinicalDbContext(DbContextOptions<ClinicalDbContext> options) : DbContext(options), IDbContext
    {
        private static readonly ValueComparer<Formula> s_formulaComparer = new(
            (l, r) => (l == null && r == null) || (l != null && r != null && FormulaSerializer.Serialize(l) == FormulaSerializer.Serialize(r)),
            v => v == null ? 0 : FormulaSerializer.Serialize(v).GetHashCode(),
            v => v);

        private IExecutionStrategy GetExecutionStrategy() => base.Database.CreateExecutionStrategy();

        public DbSet<ClinicalVariable> ClinicalVariables { get; set; }
        public DbSet<Criterion> Criteria { get; set; }
        public DbSet<Pathogen> Pathogens { get; set; }
        public DbSet<RiskFactor> RiskFactors { get; set; }
        public DbSet<ScoreMetrics> ScoreMetrics { get; set; }
        public DbSet<ScoringRule> ScoringRules { get; set; }
        public DbSet<SuspectedCause> SuspectedCauses { get; set; }

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

            // Apply query filter to soft delete items
            modelBuilder.Entity<ClinicalVariable>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Criterion>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Pathogen>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<RiskFactor>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<ScoreMetrics>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<ScoringRule>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<SuspectedCause>().HasQueryFilter(x => !x.IsDeleted);

            // Config on clinical variable
            modelBuilder.Entity<ClinicalVariable>().ToTable("clinical_variables");
            modelBuilder.Entity<ClinicalVariable>().Property(x => x.ValueType).HasConversion<string>();

            modelBuilder.Entity<Criterion>().ToTable("criteria");
            modelBuilder.Entity<Criterion>().Ignore(x => x.Variables);
            modelBuilder.Entity<Criterion>().Property(x => x.Formula)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => FormulaSerializer.Serialize(v),
                    v => FormulaSerializer.Deserialize(v))
                .Metadata.SetValueComparer(s_formulaComparer);

            // Config on pathogen
            modelBuilder.Entity<Pathogen>().ToTable("pathogens");

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

            // Config on score metrics
            modelBuilder.Entity<ScoreMetrics>().ToTable("score_metrics");

            // Config on scoring rule
            modelBuilder.Entity<ScoringRule>().ToTable("scoring_rules");
            modelBuilder.Entity<ScoringRule>()
                .HasOne(x => x.ScoreMetrics)
                .WithMany(x => x.ScoringRules)
                .HasForeignKey(x => x.ScoreMetricsId);
            modelBuilder.Entity<ScoringRule>()
                .HasOne(x => x.Criterion)
                .WithMany()
                .HasForeignKey(x => x.CriterionId);
            modelBuilder.Entity<ScoringRule>().Ignore(x => x.Variables);
            modelBuilder.Entity<ScoringRule>().Property(x => x.ScoreFunction)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => FormulaSerializer.Serialize(v),
                    v => FormulaSerializer.Deserialize(v))
                .Metadata.SetValueComparer(s_formulaComparer);
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
        }
    }
}
