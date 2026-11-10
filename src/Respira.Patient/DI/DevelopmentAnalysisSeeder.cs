using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Respira.Patient.Domain.Entities;
using PatientEntity = Respira.Patient.Domain.Entities.Patient;
using Respira.Patient.Domain.Enums;
using Respira.Patient.Domain.Models;
using Respira.Patient.Infrastructure.Data;
using Respira.ServiceDefaults.Auditing;

namespace Respira.Patient.DI;

/// <summary>Deterministic development records for exercising Analysis reports and Patient AuditLogs.</summary>
public static class DevelopmentAnalysisSeeder
{
    public static readonly Guid DoctorOneId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid DoctorTwoId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid HeadDoctorId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid MedicineOneId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid MedicineTwoId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static readonly Guid PatientUpdateAuditId = Guid.Parse("92000000-0000-0000-0000-000000000001");
    public static readonly Guid TreatmentPlanReviewAuditId = Guid.Parse("92000000-0000-0000-0000-000000000002");

    private static readonly Guid DiagnosisId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public static async Task SeedAnalysisDataAsync(this IHost host)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PatientDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PatientDbContext>>();

        var patients = SamplePatients();
        var seededPatientIds = patients.Select(patient => patient.Id).ToArray();
        var existingPatientIds = await db.Patients
            .IgnoreQueryFilters()
            .Where(patient => seededPatientIds.Contains(patient.Id))
            .Select(patient => patient.Id)
            .ToListAsync();
        var existingPatientIdSet = existingPatientIds.ToHashSet();

        var missingPatients = patients
            .Where(patient => !existingPatientIdSet.Contains(patient.Id))
            .ToList();
        if (missingPatients.Count > 0)
            await db.Patients.AddRangeAsync(missingPatients);

        var plans = SampleTreatmentPlans();
        var seededPlanIds = plans.Select(plan => plan.Id).ToArray();
        var existingPlanIds = await db.TreatmentPlans
            .IgnoreQueryFilters()
            .Where(plan => seededPlanIds.Contains(plan.Id))
            .Select(plan => plan.Id)
            .ToListAsync();
        var existingPlanIdSet = existingPlanIds.ToHashSet();

        var missingPlans = plans.Where(plan => !existingPlanIdSet.Contains(plan.Id)).ToList();
        if (missingPlans.Count > 0)
            await db.TreatmentPlans.AddRangeAsync(missingPlans);

        if (missingPatients.Count > 0 || missingPlans.Count > 0)
        {
            using (
                AuditContextScope.Begin(
                    new AuditContext(
                        DoctorOneId,
                        "Doctor",
                        Guid.Parse("99100000-0000-0000-0000-000000000000"),
                        "/api/v1/patients",
                        "seed-patient-initial"
                    )
                )
            )
            {
                await db.SaveChangesAsync();
            }

            logger.LogInformation(
                "Seeded Analysis sample data: {PatientCount} patients, {PlanCount} closed plans; Doctor IDs {DoctorOneId} and {DoctorTwoId}; Medicine IDs {MedicineOneId} and {MedicineTwoId}",
                missingPatients.Count,
                missingPlans.Count,
                DoctorOneId,
                DoctorTwoId,
                MedicineOneId,
                MedicineTwoId
            );
        }

        await SeedPatientAuditDemoAsync(db, logger);
    }

    private static async Task SeedPatientAuditDemoAsync(
        PatientDbContext db,
        ILogger logger
    )
    {
        if (await db.AuditEntries.AnyAsync(entry => entry.Id == PatientUpdateAuditId))
        {
            return;
        }

        var firstPatientId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var firstPatient = await db.Patients.FirstOrDefaultAsync(p => p.Id == firstPatientId);
        if (firstPatient is not null)
        {
            using (
                AuditContextScope.Begin(
                    new AuditContext(
                        DoctorOneId,
                        "Doctor",
                        Guid.Parse("99100000-0000-0000-0000-000000000001"),
                        $"/api/v1/patients/{firstPatientId}",
                        "seed-patient-update"
                    ),
                    preferredAuditEntryId: PatientUpdateAuditId,
                    preferredChangeSetId: Guid.Parse("92100000-0000-0000-0000-000000000001")
                )
            )
            {
                firstPatient.Weight = 69.5m;
                firstPatient.Address = "123 Nguyen Hue, District 1";
                firstPatient.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();
            }
        }

        var secondPlanId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var secondPlan = await db.TreatmentPlans.FirstOrDefaultAsync(p => p.Id == secondPlanId);
        if (secondPlan is not null)
        {
            using (
                AuditContextScope.Begin(
                    new AuditContext(
                        HeadDoctorId,
                        "HeadDepartmentDoctor",
                        Guid.Parse("99100000-0000-0000-0000-000000000002"),
                        $"/api/v1/treatment-plans/{secondPlanId}/review",
                        "seed-treatment-plan-review"
                    ),
                    preferredAuditEntryId: TreatmentPlanReviewAuditId,
                    preferredChangeSetId: Guid.Parse("92100000-0000-0000-0000-000000000002")
                )
            )
            {
                secondPlan.ReasonForMedicineChoice =
                    "Reviewed and approved atypical coverage (Audit Demo)";
                secondPlan.UpdatedAt = DateTimeOffset.UtcNow.AddDays(-10);
                await db.SaveChangesAsync();
            }
        }

        logger.LogInformation(
            "Seeded Patient Audit demo entries: PatientUpdate={PatientUpdateAuditId}, TreatmentPlanReview={TreatmentPlanReviewAuditId}",
            PatientUpdateAuditId,
            TreatmentPlanReviewAuditId
        );
    }

    private static List<PatientEntity> SamplePatients()
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            new PatientEntity
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                FirstName = "Minh",
                LastName = "Nguyen",
                PhoneNumber = "+84910000001",
                DateOfBirth = new DateOnly(1980, 1, 12),
                Gender = Gender.Male,
                Height = 170,
                Weight = 68,
                MedicalRecordCode = "ANALYSIS-DEMO-001",
                HealthInsuranceCard = "9000000001",
                Address = "District 1",
                City = "Ho Chi Minh City",
                Admission = now.AddDays(-35),
                Discharge = now.AddDays(-8),
                TreatmentOutcome = TreatmentOutcome.Recovered,
            },
            new PatientEntity
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                FirstName = "Lan",
                LastName = "Tran",
                PhoneNumber = "+84910000002",
                DateOfBirth = new DateOnly(1975, 4, 23),
                Gender = Gender.Female,
                Height = 158,
                Weight = 54,
                MedicalRecordCode = "ANALYSIS-DEMO-002",
                HealthInsuranceCard = "9000000002",
                Address = "Binh Thanh District",
                City = "Ho Chi Minh City",
                Admission = now.AddDays(-20),
                TreatmentOutcome = TreatmentOutcome.InTreatment,
            },
            new PatientEntity
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                FirstName = "Quang",
                LastName = "Le",
                PhoneNumber = "+84910000003",
                DateOfBirth = new DateOnly(1968, 9, 5),
                Gender = Gender.Male,
                Height = 172,
                Weight = 72,
                MedicalRecordCode = "ANALYSIS-DEMO-003",
                HealthInsuranceCard = "9000000003",
                Address = "Thu Duc City",
                City = "Ho Chi Minh City",
                Admission = now.AddDays(-18),
                Discharge = now.AddDays(-2),
                TreatmentOutcome = TreatmentOutcome.Death,
            },
            new PatientEntity
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                FirstName = "Hoa",
                LastName = "Pham",
                PhoneNumber = "+84910000004",
                DateOfBirth = new DateOnly(1992, 6, 14),
                Gender = Gender.Female,
                Height = 162,
                Weight = 57,
                MedicalRecordCode = "ANALYSIS-DEMO-004",
                HealthInsuranceCard = "9000000004",
                Address = "Go Vap District",
                City = "Ho Chi Minh City",
                Admission = now.AddDays(-10),
                TreatmentOutcome = TreatmentOutcome.OutOfScope,
            },
        ];
    }

    private static List<TreatmentPlan> SampleTreatmentPlans()
    {
        var now = DateTimeOffset.UtcNow;
        var medicineOne = new Antibiotic(MedicineOneId, "Analysis Demo Amoxicillin", "Penicillin");
        var medicineTwo = new Antibiotic(MedicineTwoId, "Analysis Demo Azithromycin", "Macrolide");

        return
        [
            Plan(1, 1, DoctorOneId, AdherenceState.Adhere, now.AddDays(-14), [medicineOne]),
            Plan(2, 2, DoctorOneId, AdherenceState.NotAdhereValid, now.AddDays(-11), [medicineOne, medicineTwo]),
            Plan(3, 1, DoctorTwoId, AdherenceState.NotAdhereInvalid, now.AddDays(-9), [medicineTwo]),
            Plan(4, 3, DoctorTwoId, AdherenceState.Adhere, now.AddDays(-6), [medicineTwo]),
            Plan(5, 4, DoctorTwoId, AdherenceState.NotAdhere, now.AddDays(-3), [medicineOne]),
        ];

        static TreatmentPlan Plan(
            int planNumber,
            int patientNumber,
            Guid doctorId,
            AdherenceState adherence,
            DateTimeOffset updatedAt,
            List<Antibiotic> medicines
        ) => new()
        {
            Id = Guid.Parse($"20000000-0000-0000-0000-{planNumber:000000000000}"),
            PatientId = Guid.Parse($"10000000-0000-0000-0000-{patientNumber:000000000000}"),
            DoctorId = doctorId,
            DiagnosisId = DiagnosisId,
            Diagnosis = "Analysis development sample",
            Medicines = medicines,
            AdherenceState = adherence,
            Status = TreatmentPlanStatus.Closed,
            CreatedAt = updatedAt,
            UpdatedAt = updatedAt,
        };
    }
}
