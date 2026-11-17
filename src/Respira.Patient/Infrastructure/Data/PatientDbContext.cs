using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Domain.Entities;
using Respira.ServiceDefaults.Auditing;
using Respira.ServiceDefaults.Utils.FullTextSearch;

namespace Respira.Patient.Infrastructure.Data
{
    public class PatientDbContext(
        DbContextOptions<PatientDbContext> options,
        IAuditContextAccessor? auditContextAccessor = null
    ) : AuditableDbContext(options, "Patient", auditContextAccessor), IDbContext
    {
        private IExecutionStrategy GetExecutionStrategy() => base.Database.CreateExecutionStrategy();

        public DbSet<Domain.Entities.Patient> Patients { get; set; }
        public DbSet<NextOfKin> NextOfKins { get; set; }
        public DbSet<TreatmentPlan> TreatmentPlans { get; set; }

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ConfigureAuditEntries();

            // Accent-insensitive full text search. The "unaccent" dictionary is
            // chained in front of the "simple" one via the custom "respira_vi"
            // text search configuration
            modelBuilder.HasPostgresExtension("unaccent");

            modelBuilder.Entity<Domain.Entities.Patient>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<NextOfKin>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<TreatmentPlan>().HasQueryFilter(x => !x.IsDeleted);

            // Config on patient
            modelBuilder.Entity<Domain.Entities.Patient>().ToTable("patients");
            modelBuilder.Entity<Domain.Entities.Patient>().Property(x => x.Gender).HasConversion<string>();
            modelBuilder.Entity<Domain.Entities.Patient>().Property(x => x.TreatmentOutcome).HasConversion<string>();
            modelBuilder.Entity<Domain.Entities.Patient>().Ignore(x => x.FullName);
            modelBuilder.Entity<Domain.Entities.Patient>()
                .HasGeneratedTsVectorColumn(
                    x => x.SearchVector,
                    FullTextSearchUtil.ConfigurationName,
                    x => new { x.FirstName, x.LastName, x.MedicalRecordCode })
                .HasIndex(x => x.SearchVector)
                .HasMethod("GIN");


            // Config on next of kin
            modelBuilder.Entity<NextOfKin>().ToTable("next_of_kins");
            modelBuilder.Entity<NextOfKin>()
                .HasOne(x => x.Patient)
                .WithMany(x => x.NextOfKins)
                .HasForeignKey(x => x.PatientId);

            // Config on treatment
            modelBuilder.Entity<TreatmentPlan>().ToTable("treatment_plans");
            modelBuilder.Entity<TreatmentPlan>()
                .HasOne(x => x.Patient)
                .WithMany(x => x.TreatmentPlans)
                .HasForeignKey(x => x.PatientId);
            modelBuilder.Entity<TreatmentPlan>().Property(x => x.AdherenceState).HasConversion<string>();
            modelBuilder.Entity<TreatmentPlan>().Property(x => x.Status).HasConversion<string>();
            modelBuilder.Entity<TreatmentPlan>()
                .OwnsMany(x => x.Medicines, builder => builder.ToJson());
        }
    }
}
