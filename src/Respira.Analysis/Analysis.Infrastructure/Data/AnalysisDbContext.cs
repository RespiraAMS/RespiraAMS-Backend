using Microsoft.EntityFrameworkCore;
using Respira.Analysis.Application.Contracts;
using Respira.Analysis.Domain.Entities;
using Respira.ServiceDefaults.Auditing;

namespace Respira.Analysis.Infrastructure.Data
{
    public sealed class AnalysisDbContext(
        DbContextOptions<AnalysisDbContext> options,
        IAuditContextAccessor? auditContextAccessor = null
    ) : AuditableDbContext(options, "Analysis", auditContextAccessor), IAnalysisDbContext
    {
        public DbSet<ClinicalAnalysis> ClinicalAnalyses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ConfigureAuditEntries();
            modelBuilder.Entity<ClinicalAnalysis>(entity =>
            {
                entity.ToTable("clinical_analyses");
                entity.Property(item => item.PatientIdsJson).HasColumnType("jsonb");
                entity.Property(item => item.TreatmentPlanIdsJson).HasColumnType("jsonb");
                entity.Property(item => item.ReportJson).HasColumnType("jsonb");
                entity.HasIndex(item => item.CreatedAt);
            });
        }
    }
}
