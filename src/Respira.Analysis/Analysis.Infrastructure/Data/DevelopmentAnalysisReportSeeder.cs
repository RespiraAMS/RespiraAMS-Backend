using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Respira.Analysis.Application.Contracts;
using Respira.Analysis.Domain.Entities;
using Respira.ServiceDefaults.Auditing;

namespace Respira.Analysis.Infrastructure.Data;

/// <summary>
/// Seeds deterministic Analysis reports and AuditLog entries in Development.
/// </summary>
public static class DevelopmentAnalysisReportSeeder
{
    public static readonly Guid AdminActorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DoctorOneId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid DoctorTwoId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid MedicineOneId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid MedicineTwoId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static readonly Guid DoctorReportId = Guid.Parse("70000000-0000-0000-0000-000000000001");
    public static readonly Guid SystemReportId = Guid.Parse("70000000-0000-0000-0000-000000000002");
    public static readonly Guid SoftDeletedReportId = Guid.Parse("70000000-0000-0000-0000-000000000003");

    public static readonly Guid DoctorReportCreateAuditId = Guid.Parse("93000000-0000-0000-0000-000000000001");
    public static readonly Guid SystemReportCreateAuditId = Guid.Parse("93000000-0000-0000-0000-000000000002");
    public static readonly Guid ReportSoftDeleteAuditId = Guid.Parse("93000000-0000-0000-0000-000000000003");

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task SeedAnalysisReportsAsync(this IHost host)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AnalysisDbContext>>();

        if (
            await db
                .ClinicalAnalyses.IgnoreQueryFilters()
                .AnyAsync(item => item.Id == DoctorReportId)
        )
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var start = now.AddDays(-30);
        var end = now;

        var doctorOneSummary = new DoctorMedicineAnalysisSummary(
            DoctorOneId,
            2,
            2,
            1,
            1,
            0,
            [
                new MedicineAnalysisSummary(MedicineOneId, "Analysis Demo Amoxicillin", "Penicillin", 2, 1, 1, 0),
                new MedicineAnalysisSummary(MedicineTwoId, "Analysis Demo Azithromycin", "Macrolide", 1, 0, 1, 0),
            ]
        );

        var doctorTwoSummary = new DoctorMedicineAnalysisSummary(
            DoctorTwoId,
            3,
            3,
            1,
            1,
            1,
            [
                new MedicineAnalysisSummary(MedicineOneId, "Analysis Demo Amoxicillin", "Penicillin", 1, 0, 0, 1),
                new MedicineAnalysisSummary(MedicineTwoId, "Analysis Demo Azithromycin", "Macrolide", 2, 1, 1, 0),
            ]
        );

        var doctorReportResult = new AnalysisReportResult(
            DoctorReportId,
            DoctorOneId,
            start,
            end,
            2,
            2,
            1,
            1,
            0,
            0,
            1,
            1,
            [doctorOneSummary]
        );

        var systemReportResult = new AnalysisReportResult(
            SystemReportId,
            null,
            start,
            end,
            5,
            4,
            2,
            2,
            1,
            1,
            1,
            2,
            [doctorOneSummary, doctorTwoSummary]
        );

        var softDeletedReportResult = systemReportResult with { AnalysisId = SoftDeletedReportId };

        // 1. Seed DoctorOne report
        using (
            AuditContextScope.Begin(
                new AuditContext(
                    DoctorOneId,
                    "Doctor",
                    Guid.Parse("99200000-0000-0000-0000-000000000001"),
                    "/api/v1/analysis/reports",
                    "seed-analysis-doctor-report"
                ),
                preferredAuditEntryId: DoctorReportCreateAuditId,
                preferredChangeSetId: Guid.Parse("93100000-0000-0000-0000-000000000001")
            )
        )
        {
            db.ClinicalAnalyses.Add(
                new ClinicalAnalysis
                {
                    Id = DoctorReportId,
                    DoctorId = DoctorOneId,
                    Start = start,
                    End = end,
                    PatientIdsJson = "[\"10000000-0000-0000-0000-000000000001\",\"10000000-0000-0000-0000-000000000002\"]",
                    TreatmentPlanIdsJson = "[\"20000000-0000-0000-0000-000000000001\",\"20000000-0000-0000-0000-000000000002\"]",
                    ReportJson = JsonSerializer.Serialize(doctorReportResult, JsonOptions),
                }
            );
            await db.SaveChangesAsync();
        }

        // 2. Seed System-wide report + report to be soft-deleted
        var toDelete = new ClinicalAnalysis
        {
            Id = SoftDeletedReportId,
            DoctorId = DoctorTwoId,
            Start = start,
            End = end,
            PatientIdsJson = "[\"10000000-0000-0000-0000-000000000003\"]",
            TreatmentPlanIdsJson = "[\"20000000-0000-0000-0000-000000000004\"]",
            ReportJson = JsonSerializer.Serialize(softDeletedReportResult, JsonOptions),
        };

        using (
            AuditContextScope.Begin(
                new AuditContext(
                    AdminActorId,
                    "Admin",
                    Guid.Parse("99200000-0000-0000-0000-000000000002"),
                    "/api/v1/analysis/reports",
                    "seed-analysis-system-report"
                ),
                preferredAuditEntryId: SystemReportCreateAuditId,
                preferredChangeSetId: Guid.Parse("93100000-0000-0000-0000-000000000002")
            )
        )
        {
            db.ClinicalAnalyses.AddRange(
                new ClinicalAnalysis
                {
                    Id = SystemReportId,
                    DoctorId = null,
                    Start = start,
                    End = end,
                    PatientIdsJson = "[\"10000000-0000-0000-0000-000000000001\",\"10000000-0000-0000-0000-000000000002\",\"10000000-0000-0000-0000-000000000003\",\"10000000-0000-0000-0000-000000000004\"]",
                    TreatmentPlanIdsJson = "[\"20000000-0000-0000-0000-000000000001\",\"20000000-0000-0000-0000-000000000002\",\"20000000-0000-0000-0000-000000000003\",\"20000000-0000-0000-0000-000000000004\",\"20000000-0000-0000-0000-000000000005\"]",
                    ReportJson = JsonSerializer.Serialize(systemReportResult, JsonOptions),
                },
                toDelete
            );
            await db.SaveChangesAsync();
        }

        // 3. Soft-delete the third report to record Analysis.ClinicalAnalysis.SoftDeleted
        using (
            AuditContextScope.Begin(
                new AuditContext(
                    AdminActorId,
                    "Admin",
                    Guid.Parse("99200000-0000-0000-0000-000000000003"),
                    $"/api/v1/analysis/reports/{SoftDeletedReportId}",
                    "seed-analysis-soft-delete"
                ),
                preferredAuditEntryId: ReportSoftDeleteAuditId,
                preferredChangeSetId: Guid.Parse("93100000-0000-0000-0000-000000000003")
            )
        )
        {
            toDelete.IsDeleted = true;
            toDelete.DeletedAt = DateTimeOffset.UtcNow;
            toDelete.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        logger.LogInformation(
            "Seeded Analysis sample reports and AuditLog entries: DoctorReport={DoctorReportId} (Audit={DoctorReportCreateAuditId}), SystemReport={SystemReportId} (Audit={SystemReportCreateAuditId}), SoftDeletedReport={SoftDeletedReportId} (Audit={ReportSoftDeleteAuditId})",
            DoctorReportId,
            DoctorReportCreateAuditId,
            SystemReportId,
            SystemReportCreateAuditId,
            SoftDeletedReportId,
            ReportSoftDeleteAuditId
        );
    }
}
