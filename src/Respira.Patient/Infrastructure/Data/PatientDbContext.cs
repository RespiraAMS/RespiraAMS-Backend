using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Domain.Entities;

namespace Respira.Patient.Infrastructure.Data
{
    public class PatientDbContext(DbContextOptions<PatientDbContext> options) : DbContext(options), IDbContext
    {
        private IExecutionStrategy GetExecutionStrategy() => base.Database.CreateExecutionStrategy();

        public DbSet<Domain.Entities.Patient> Patients { get; set; }
        public DbSet<NextOfKin> NextOfKins { get; set; }
        public DbSet<Treatment> Treatments { get; set; }

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

            modelBuilder.Entity<Domain.Entities.Patient>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<NextOfKin>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Treatment>().HasQueryFilter(x => !x.IsDeleted);

            // Config on patient
            modelBuilder.Entity<Domain.Entities.Patient>().ToTable("patients");
            modelBuilder.Entity<Domain.Entities.Patient>().Property(x => x.Gender).HasConversion<string>();
            modelBuilder.Entity<Domain.Entities.Patient>().Property(x => x.TreatmentOutcome).HasConversion<string>();
            modelBuilder.Entity<Domain.Entities.Patient>().Ignore(x => x.Age);
            modelBuilder.Entity<Domain.Entities.Patient>().Ignore(x => x.FullName);

            // Config on next of kin
            modelBuilder.Entity<NextOfKin>().ToTable("next_of_kins");
            modelBuilder.Entity<NextOfKin>()
                .HasOne(x => x.Patient)
                .WithMany(x => x.NextOfKins)
                .HasForeignKey(x => x.PatientId);

            // Config on treatment
            modelBuilder.Entity<Treatment>().ToTable("treatments");
            modelBuilder.Entity<Treatment>()
                .HasOne(x => x.Patient)
                .WithMany(x => x.Treatments)
                .HasForeignKey(x => x.PatientId);
        }
    }
}
