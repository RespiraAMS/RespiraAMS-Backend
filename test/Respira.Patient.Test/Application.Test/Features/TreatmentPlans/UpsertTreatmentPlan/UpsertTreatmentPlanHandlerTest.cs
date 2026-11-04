using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Application.Features.TreatmentPlans.UpsertTreatmentPlan;
using Respira.Patient.Domain.Entities;
using Respira.Patient.Domain.Enums;
using Respira.Patient.Domain.Models;
using Respira.Patient.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Test.Features.TreatmentPlans.UpsertTreatmentPlan
{
    public class UpsertTreatmentPlanHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        // Fixed timestamps so an update can be proven to bump the row while keeping
        // the original creation time
        private static readonly DateTimeOffset SeedCreatedAt = new(2026, 1, 15, 8, 30, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset SeedUpdatedAt = new(2026, 1, 16, 9, 45, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset SeedDeletedAt = new(2026, 9, 30, 14, 5, 0, TimeSpan.Zero);

        private readonly DbContextOptions<PatientDbContext> _options;
        private readonly UpsertTreatmentPlanHandler _handler;
        private readonly IDbContext _context;

        public UpsertTreatmentPlanHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<PatientDbContext>()
                .UseNpgsql(fixture.ConnectionString).Options;
            _context = new PatientDbContext(_options);
            var logger = new Mock<ILogger<UpsertTreatmentPlanHandler>>().Object;

            // Initialize handler
            _handler = new(_context, logger);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Children reference patients through FKs, so delete them first.
            // IgnoreQueryFilters is needed because soft-deleted rows are hidden by the
            // query filter but still occupy the table
            await _context.TreatmentPlans.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.NextOfKins.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Patients.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        #region Seed helpers

        private sealed record PatientSeed(
            Domain.Entities.Patient Main,
            Domain.Entities.Patient Other,
            Domain.Entities.Patient Removed);

        private static Domain.Entities.Patient Patient(
            string firstName,
            string lastName,
            string phoneNumber,
            string medicalRecordCode,
            string healthInsuranceCard,
            DateOnly dateOfBirth,
            Gender gender,
            decimal height,
            decimal weight,
            string address,
            string city,
            bool softDeleted = false)
        {
            return new Domain.Entities.Patient
            {
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                DateOfBirth = dateOfBirth,
                Gender = gender,
                Height = height,
                Weight = weight,
                MedicalRecordCode = medicalRecordCode,
                HealthInsuranceCard = healthInsuranceCard,
                Address = address,
                City = city,
                Admission = SeedCreatedAt,
                TreatmentOutcome = TreatmentOutcome.InTreatment,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? SeedDeletedAt : null,
            };
        }

        private async Task<PatientSeed> SeedPatientsAsync()
        {
            // Committed through a separate context so the handler's change tracker
            // never sees the rows and only the database can satisfy the lookups
            await using var context = new PatientDbContext(_options);

            var main = Patient(
                "Thanh Tùng",
                "Võ",
                "0903482715",
                "BN-2026-004912",
                "7016382471",
                new DateOnly(1968, 3, 17),
                Gender.Male,
                168.4m,
                64.2m,
                "45 Hai Bà Trưng, quận Hoàn Kiếm",
                "Hà Nội");
            var other = Patient(
                "Hoài An",
                "Trịnh Thị",
                "0987164530",
                "BN-2026-005376",
                "8816372058",
                new DateOnly(1994, 9, 27),
                Gender.Female,
                158.6m,
                51.3m,
                "12 Trần Phú, quận Ninh Kiều",
                "Cần Thơ");
            var removed = Patient(
                "Hồng",
                "Vũ Thị",
                "0907835412",
                "BN-2026-007315",
                "8816372059",
                new DateOnly(1976, 11, 2),
                Gender.Female,
                154.7m,
                57.8m,
                "18 Lý Thái Tổ, quận Hoàn Kiếm",
                "Hà Nội",
                softDeleted: true);

            await context.Patients.AddRangeAsync(
                [main, other, removed], TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            return new PatientSeed(main, other, removed);
        }

        private async Task<TreatmentPlan> SeedPlanAsync(
            Guid patientId,
            Guid diagnosisId,
            TreatmentPlanStatus status = TreatmentPlanStatus.InProgress,
            AdherenceState adherenceState = AdherenceState.Adhere,
            string? reasonForMedicineChoice = null,
            Guid? doctorId = null,
            string? diagnosis = null,
            List<Antibiotic>? medicines = null,
            DateTimeOffset? createdAt = null,
            DateTimeOffset? updatedAt = null,
            bool softDeleted = false)
        {
            var plan = new TreatmentPlan
            {
                PatientId = patientId,
                DoctorId = doctorId ?? Guid.CreateVersion7(),
                DiagnosisId = diagnosisId,
                Diagnosis = diagnosis ?? DiagnosisJson(diagnosisId),
                Medicines = medicines ??
                [
                    new Antibiotic(Guid.CreateVersion7(), "Ceftriaxone", "Watch"),
                ],
                ReasonForMedicineChoice = reasonForMedicineChoice,
                AdherenceState = adherenceState,
                Status = status,
                CloseReason = status == TreatmentPlanStatus.Closed
                    ? "Patient was transferred to a provincial hospital"
                    : null,
                CreatedAt = createdAt ?? SeedCreatedAt,
                UpdatedAt = updatedAt ?? SeedUpdatedAt,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? SeedDeletedAt : null,
            };

            await using var context = new PatientDbContext(_options);
            context.TreatmentPlans.Add(plan);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return plan;
        }

        #endregion

        #region Command helpers

        private static string DiagnosisJson(Guid diagnosisId)
        {
            return $$"""
                {
                  "diagnosisId": "{{diagnosisId}}",
                  "severity": "Severe",
                  "treatmentSite": "Inpatient",
                  "egfr": 48.5,
                  "isPatientPregnantOrInLactationPhase": false
                }
                """;
        }

        private static List<Antibiotic> TwoMedicineCombination()
        {
            // Conventional moderate CAP therapy: third-generation cephalosporin
            // plus a macrolide
            return
            [
                new Antibiotic(Guid.CreateVersion7(), "Ceftriaxone", "Watch"),
                new Antibiotic(Guid.CreateVersion7(), "Azithromycin", "Watch"),
            ];
        }

        private static List<Antibiotic> ThreeMedicineCombination()
        {
            // Maximal realistic empiric pick for severe pneumonia with suspected
            // atypical and MRSA cover
            return
            [
                new Antibiotic(Guid.CreateVersion7(), "Meropenem", "Reserve"),
                new Antibiotic(Guid.CreateVersion7(), "Azithromycin", "Watch"),
                new Antibiotic(Guid.CreateVersion7(), "Vancomycin", "Watch"),
            ];
        }

        private static UpsertTreatmentPlanCommand CreateCommand(
            Guid patientId,
            Guid diagnosisId,
            Guid doctorId,
            bool isAdhere = true,
            string? reasonForMedicineChoice = null,
            string? diagnosis = null,
            List<Antibiotic>? medicines = null)
        {
            return new UpsertTreatmentPlanCommand
            {
                DoctorId = doctorId,
                PatientId = patientId,
                DiagnosisId = diagnosisId,
                Diagnosis = diagnosis ?? DiagnosisJson(diagnosisId),
                Medicines = medicines ?? TwoMedicineCombination(),
                ReasonForMedicineChoice = reasonForMedicineChoice,
                IsAdhere = isAdhere,
            };
        }

        private async Task<TreatmentPlan> LoadPlanAsync(Guid id)
        {
            // Fresh context so the handler's change tracker cannot mask a failed commit
            await using var context = new PatientDbContext(_options);
            return await context.TreatmentPlans.SingleAsync(
                x => x.Id == id, TestContext.Current.CancellationToken);
        }

        private async Task<int> CountPlansAsync(Guid? patientId = null)
        {
            await using var context = new PatientDbContext(_options);
            var query = context.TreatmentPlans.IgnoreQueryFilters();
            if (patientId is not null)
            {
                query = query.Where(x => x.PatientId == patientId);
            }

            return await query.CountAsync(TestContext.Current.CancellationToken);
        }

        private static void AssertSuccess(Result<UpsertTreatmentPlanResult> result)
        {
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.NotEqual(Guid.Empty, result.Data.Id);
        }

        private static void AssertFailure(Result<UpsertTreatmentPlanResult> result)
        {
            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Equal(ApplicationStatus.BadRequest, result.Error.StatusCode);
        }

        #endregion

        #region Happy path

        [Fact]
        public async Task UpsertTreatmentPlan_NewPlanAdhere_CreatesPlan()
        {
            // Boundary: 1 medicine is the smallest accepted treatment plan
            var patients = await SeedPatientsAsync();
            var diagnosisId = Guid.CreateVersion7();
            var doctorId = Guid.CreateVersion7();
            var medicine = new Antibiotic(Guid.CreateVersion7(), "Amoxicillin", "Access");
            var command = CreateCommand(
                patients.Main.Id,
                diagnosisId,
                doctorId,
                isAdhere: true,
                reasonForMedicineChoice: null,
                medicines: [medicine]);
            var before = DateTimeOffset.UtcNow;

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            AssertSuccess(result);
            var saved = await LoadPlanAsync(result.Data!.Id);

            // Business rule: a new plan is created when the patient has no plan for
            // this diagnosis, with the command values and InProgress status
            Assert.Equal(command.DoctorId, saved.DoctorId);
            Assert.Equal(command.PatientId, saved.PatientId);
            Assert.Equal(command.DiagnosisId, saved.DiagnosisId);
            Assert.Equal(command.Diagnosis, saved.Diagnosis);
            Assert.Equal(command.Medicines, saved.Medicines);
            Assert.Null(saved.ReasonForMedicineChoice);
            Assert.Equal(AdherenceState.Adhere, saved.AdherenceState);
            Assert.Equal(TreatmentPlanStatus.InProgress, saved.Status);
            Assert.Null(saved.CloseReason);
            Assert.False(saved.IsDeleted);
            Assert.InRange(saved.CreatedAt, before.AddMilliseconds(-1), DateTimeOffset.UtcNow.AddMilliseconds(1));
            Assert.InRange(saved.UpdatedAt, before.AddMilliseconds(-1), DateTimeOffset.UtcNow.AddMilliseconds(1));

            Assert.Equal(1, await CountPlansAsync(patients.Main.Id));
        }

        [Fact]
        public async Task UpsertTreatmentPlan_NewPlanNotAdhereWithReason_CreatesPlan()
        {
            // Boundary: 3 medicines is the largest realistic custom combination.
            // A non-adherent pick must carry a reason and is stored as NotAdhere
            var patients = await SeedPatientsAsync();
            var diagnosisId = Guid.CreateVersion7();
            var doctorId = Guid.CreateVersion7();
            const string reason =
                "Blood culture grew Pseudomonas aeruginosa; escalated to meropenem and added vancomycin for suspected MRSA";
            var command = CreateCommand(
                patients.Main.Id,
                diagnosisId,
                doctorId,
                isAdhere: false,
                reasonForMedicineChoice: reason,
                medicines: ThreeMedicineCombination());

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            AssertSuccess(result);
            var saved = await LoadPlanAsync(result.Data!.Id);

            Assert.Equal(command.Medicines, saved.Medicines);
            Assert.Equal(3, saved.Medicines.Count);
            Assert.Equal(reason, saved.ReasonForMedicineChoice);
            Assert.Equal(AdherenceState.NotAdhere, saved.AdherenceState);
            Assert.Equal(TreatmentPlanStatus.InProgress, saved.Status);

            Assert.Equal(1, await CountPlansAsync(patients.Main.Id));
        }

        [Fact]
        public async Task UpsertTreatmentPlan_RawDiagnosis_ForwardedVerbatim()
        {
            // The diagnosis snapshot is produced by the clinical service and must be
            // persisted exactly as received, never re-serialized
            var patients = await SeedPatientsAsync();
            var diagnosisId = Guid.CreateVersion7();
            var rawDiagnosis = $$"""
                {
                  "diagnosisId": "{{diagnosisId}}",
                  "severity": "Severe",
                  "treatmentSite": "IntensiveCareUnit",
                  "egfr": 32.7,
                  "isPatientPregnantOrInLactationPhase": false,
                  "protocols": [
                    {
                      "id": "{{Guid.CreateVersion7()}}",
                      "medicines": [
                        [
                          { "id": "{{Guid.CreateVersion7()}}", "name": "Meropenem", "classification": "Reserve" }
                        ]
                      ]
                    }
                  ]
                }
                """;
            var command = CreateCommand(
                patients.Main.Id,
                diagnosisId,
                Guid.CreateVersion7(),
                diagnosis: rawDiagnosis);

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            AssertSuccess(result);
            var saved = await LoadPlanAsync(result.Data!.Id);
            Assert.Equal(rawDiagnosis, saved.Diagnosis);
        }

        [Fact]
        public async Task UpsertTreatmentPlan_ExistingInProgressPlan_UpdatesSamePlan()
        {
            /*
             * Business rule: the plan is upserted by patient + diagnosis. An existing
             * InProgress plan must be updated in place (same id, no extra row),
             * including a reviewed adherence state being overwritten by the new
             * confirmation, and UpdatedAt being bumped
             */
            var patients = await SeedPatientsAsync();
            var diagnosisId = Guid.CreateVersion7();
            var seeded = await SeedPlanAsync(
                patients.Main.Id,
                diagnosisId,
                adherenceState: AdherenceState.NotAdhereValid,
                reasonForMedicineChoice: "Initial empirical therapy",
                updatedAt: SeedUpdatedAt);

            var doctorId = Guid.CreateVersion7();
            const string reason = "Culture confirmed Klebsiella pneumoniae; de-escalated to ceftriaxone";
            var command = CreateCommand(
                patients.Main.Id,
                diagnosisId,
                doctorId,
                isAdhere: false,
                reasonForMedicineChoice: reason,
                medicines: TwoMedicineCombination());

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            AssertSuccess(result);
            Assert.Equal(seeded.Id, result.Data!.Id);

            var saved = await LoadPlanAsync(seeded.Id);
            Assert.Equal(seeded.Id, saved.Id);
            Assert.Equal(command.DoctorId, saved.DoctorId);
            Assert.Equal(patients.Main.Id, saved.PatientId);
            Assert.Equal(diagnosisId, saved.DiagnosisId);
            Assert.Equal(command.Diagnosis, saved.Diagnosis);
            Assert.Equal(command.Medicines, saved.Medicines);
            Assert.Equal(reason, saved.ReasonForMedicineChoice);
            Assert.Equal(AdherenceState.NotAdhere, saved.AdherenceState);
            Assert.Equal(TreatmentPlanStatus.InProgress, saved.Status);
            Assert.Null(saved.CloseReason);

            // The row keeps its creation time but the update timestamp is refreshed
            Assert.Equal(SeedCreatedAt, saved.CreatedAt);
            Assert.True(saved.UpdatedAt > SeedUpdatedAt);

            Assert.Equal(1, await CountPlansAsync(patients.Main.Id));
        }

        [Fact]
        public async Task UpsertTreatmentPlan_SamePatientDifferentDiagnosis_CreatesSecondPlan()
        {
            // Boundary: the upsert key includes the diagnosis, so another diagnosis
            // for the same patient must create a separate plan instead of overwriting
            var patients = await SeedPatientsAsync();
            var seededDiagnosisId = Guid.CreateVersion7();
            var newDiagnosisId = Guid.CreateVersion7();
            var seeded = await SeedPlanAsync(patients.Main.Id, seededDiagnosisId);

            var command = CreateCommand(
                patients.Main.Id,
                newDiagnosisId,
                Guid.CreateVersion7());

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            AssertSuccess(result);
            Assert.NotEqual(seeded.Id, result.Data!.Id);

            var created = await LoadPlanAsync(result.Data.Id);
            Assert.Equal(newDiagnosisId, created.DiagnosisId);

            Assert.Equal(2, await CountPlansAsync(patients.Main.Id));
            await using var verify = new PatientDbContext(_options);
            Assert.Equal(1, await verify.TreatmentPlans.CountAsync(
                x => x.Id == seeded.Id, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task UpsertTreatmentPlan_DifferentPatientSameDiagnosis_CreatesSecondPlan()
        {
            // Boundary: the lookup is scoped to the patient, so the same diagnosis
            // in another patient must not be updated or rejected
            var patients = await SeedPatientsAsync();
            var diagnosisId = Guid.CreateVersion7();
            var seeded = await SeedPlanAsync(patients.Main.Id, diagnosisId);

            var command = CreateCommand(
                patients.Other.Id,
                diagnosisId,
                Guid.CreateVersion7());

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            AssertSuccess(result);
            Assert.NotEqual(seeded.Id, result.Data!.Id);

            var created = await LoadPlanAsync(result.Data.Id);
            Assert.Equal(patients.Other.Id, created.PatientId);
            Assert.Equal(diagnosisId, created.DiagnosisId);

            await using var verify = new PatientDbContext(_options);
            Assert.Equal(2, await verify.TreatmentPlans.IgnoreQueryFilters()
                .CountAsync(x => x.DiagnosisId == diagnosisId, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task UpsertTreatmentPlan_ClosedPlanForDifferentDiagnosis_CreatesNewPlan()
        {
            // A closed plan only blocks updates of its own diagnosis; a new diagnosis
            // for the same patient is still a fresh plan
            var patients = await SeedPatientsAsync();
            var closedDiagnosisId = Guid.CreateVersion7();
            var newDiagnosisId = Guid.CreateVersion7();
            var closed = await SeedPlanAsync(
                patients.Main.Id,
                closedDiagnosisId,
                status: TreatmentPlanStatus.Closed);

            var command = CreateCommand(
                patients.Main.Id,
                newDiagnosisId,
                Guid.CreateVersion7());

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            AssertSuccess(result);
            Assert.NotEqual(closed.Id, result.Data!.Id);

            var created = await LoadPlanAsync(result.Data.Id);
            Assert.Equal(TreatmentPlanStatus.InProgress, created.Status);
            Assert.Equal(2, await CountPlansAsync(patients.Main.Id));
        }

        [Fact]
        public async Task UpsertTreatmentPlan_SoftDeletedPlan_CreatesNewPlan()
        {
            // A soft-deleted plan is hidden by the query filter, so the upsert sees
            // no existing plan and creates a fresh one
            var patients = await SeedPatientsAsync();
            var diagnosisId = Guid.CreateVersion7();
            var deleted = await SeedPlanAsync(
                patients.Main.Id,
                diagnosisId,
                softDeleted: true);

            var command = CreateCommand(
                patients.Main.Id,
                diagnosisId,
                Guid.CreateVersion7());

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            AssertSuccess(result);
            Assert.NotEqual(deleted.Id, result.Data!.Id);

            var created = await LoadPlanAsync(result.Data.Id);
            Assert.False(created.IsDeleted);
            Assert.Equal(2, await CountPlansAsync(patients.Main.Id));
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task UpsertTreatmentPlan_UnknownPatient_Fail()
        {
            var command = CreateCommand(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7());

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            AssertFailure(result);

            // Nothing is written when the patient does not exist
            Assert.Equal(0, await CountPlansAsync());
        }

        [Fact]
        public async Task UpsertTreatmentPlan_SoftDeletedPatient_Fail()
        {
            // A soft-deleted patient is hidden by the query filter, so the lookup is
            // rejected exactly like an unknown ID
            var patients = await SeedPatientsAsync();
            var command = CreateCommand(
                patients.Removed.Id,
                Guid.CreateVersion7(),
                Guid.CreateVersion7());

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            AssertFailure(result);
            Assert.Equal(0, await CountPlansAsync());
        }

        [Fact]
        public async Task UpsertTreatmentPlan_ClosedPlan_Fail()
        {
            /*
             * Business rule: a closed treatment plan is final and must not be
             * updated. The handler must reject the request and leave the row
             * untouched, without creating a replacement plan
             */
            var patients = await SeedPatientsAsync();
            var diagnosisId = Guid.CreateVersion7();
            var doctorId = Guid.CreateVersion7();
            var seeded = await SeedPlanAsync(
                patients.Main.Id,
                diagnosisId,
                status: TreatmentPlanStatus.Closed,
                adherenceState: AdherenceState.NotAdhereInvalid,
                reasonForMedicineChoice: "Original therapy was not covered by the protocol",
                doctorId: doctorId,
                medicines: ThreeMedicineCombination());

            var command = CreateCommand(
                patients.Main.Id,
                diagnosisId,
                Guid.CreateVersion7(),
                isAdhere: true,
                reasonForMedicineChoice: null,
                medicines: TwoMedicineCombination());

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            AssertFailure(result);

            var unchanged = await LoadPlanAsync(seeded.Id);
            Assert.Equal(seeded.DoctorId, unchanged.DoctorId);
            Assert.Equal(seeded.Diagnosis, unchanged.Diagnosis);
            Assert.Equal(seeded.Medicines, unchanged.Medicines);
            Assert.Equal(seeded.ReasonForMedicineChoice, unchanged.ReasonForMedicineChoice);
            Assert.Equal(AdherenceState.NotAdhereInvalid, unchanged.AdherenceState);
            Assert.Equal(TreatmentPlanStatus.Closed, unchanged.Status);
            Assert.Equal(seeded.CloseReason, unchanged.CloseReason);
            Assert.Equal(SeedUpdatedAt, unchanged.UpdatedAt);

            Assert.Equal(1, await CountPlansAsync(patients.Main.Id));
        }

        #endregion
    }
}
