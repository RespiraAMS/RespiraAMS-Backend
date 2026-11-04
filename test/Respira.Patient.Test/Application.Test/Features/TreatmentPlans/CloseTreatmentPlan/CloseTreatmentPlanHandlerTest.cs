using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Application.Features.TreatmentPlans.CloseTreatmentPlan;
using Respira.Patient.Domain.Entities;
using Respira.Patient.Domain.Enums;
using Respira.Patient.Domain.Models;
using Respira.Patient.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Test.Features.TreatmentPlans.CloseTreatmentPlan
{
    public class CloseTreatmentPlanHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        // Fixed timestamps so a close can be proven to bump UpdatedAt while
        // keeping the original creation time
        private static readonly DateTimeOffset SeedCreatedAt = new(2026, 1, 15, 8, 30, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset SeedUpdatedAt = new(2026, 1, 16, 9, 45, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset SeedDeletedAt = new(2026, 9, 30, 14, 5, 0, TimeSpan.Zero);

        private readonly DbContextOptions<PatientDbContext> _options;
        private readonly CloseTreatmentPlanHandler _handler;
        private readonly IDbContext _context;

        public CloseTreatmentPlanHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<PatientDbContext>()
                .UseNpgsql(fixture.ConnectionString).Options;
            _context = new PatientDbContext(_options);
            var logger = new Mock<ILogger<CloseTreatmentPlanHandler>>().Object;

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

        private async Task<Domain.Entities.Patient> SeedPatientAsync()
        {
            // Committed through a separate context so the handler's change tracker
            // never sees the row and only the database can satisfy the lookup
            await using var context = new PatientDbContext(_options);

            var patient = new Domain.Entities.Patient
            {
                FirstName = "Thanh Tùng",
                LastName = "Võ",
                PhoneNumber = "0903482715",
                DateOfBirth = new DateOnly(1968, 3, 17),
                Gender = Gender.Male,
                Height = 168.4m,
                Weight = 64.2m,
                MedicalRecordCode = "BN-2026-004912",
                HealthInsuranceCard = "7016382471",
                Address = "45 Hai Bà Trưng, quận Hoàn Kiếm",
                City = "Hà Nội",
                Admission = SeedCreatedAt,
                TreatmentOutcome = TreatmentOutcome.InTreatment,
            };

            context.Patients.Add(patient);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return patient;
        }

        private async Task<TreatmentPlan> SeedPlanAsync(
            Guid patientId,
            TreatmentPlanStatus status = TreatmentPlanStatus.InProgress,
            AdherenceState adherenceState = AdherenceState.NotAdhereValid,
            string? closeReason = null,
            DateTimeOffset? createdAt = null,
            DateTimeOffset? updatedAt = null,
            bool softDeleted = false)
        {
            var diagnosisId = Guid.CreateVersion7();
            var plan = new TreatmentPlan
            {
                PatientId = patientId,
                DoctorId = Guid.CreateVersion7(),
                DiagnosisId = diagnosisId,
                Diagnosis = DiagnosisJson(diagnosisId),
                Medicines =
                [
                    new Antibiotic(Guid.CreateVersion7(), "Meropenem", "Reserve"),
                    new Antibiotic(Guid.CreateVersion7(), "Azithromycin", "Watch"),
                    new Antibiotic(Guid.CreateVersion7(), "Vancomycin", "Watch"),
                ],
                ReasonForMedicineChoice =
                    "Blood culture grew Pseudomonas aeruginosa; escalated to meropenem and added vancomycin for suspected MRSA",
                AdherenceState = adherenceState,
                Status = status,
                CloseReason = closeReason,
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

        private async Task<TreatmentPlan> LoadPlanAsync(Guid id)
        {
            // Fresh context so the handler's change tracker cannot mask a failed commit
            await using var context = new PatientDbContext(_options);
            return await context.TreatmentPlans.SingleAsync(
                x => x.Id == id, TestContext.Current.CancellationToken);
        }

        private async Task<TreatmentPlan> LoadPlanIgnoringFiltersAsync(Guid id)
        {
            await using var context = new PatientDbContext(_options);
            return await context.TreatmentPlans.IgnoreQueryFilters().SingleAsync(
                x => x.Id == id, TestContext.Current.CancellationToken);
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

        private static CloseTreatmentPlanCommand CreateCommand(
            Guid treatmentPlanId,
            string reason)
        {
            return new CloseTreatmentPlanCommand
            {
                TreatmentPlanId = treatmentPlanId,
                Reason = reason,
            };
        }

        private static void AssertSuccess(Result result)
        {
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);
        }

        private static void AssertFailure(Result result)
        {
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Equal(ApplicationStatus.BadRequest, result.Error.StatusCode);
        }

        #endregion

        #region Happy path

        [Fact]
        public async Task CloseTreatmentPlan_InProgressPlan_ClosesPlan()
        {
            // Business rule: an open plan can be closed once with the doctor's reason
            var patient = await SeedPatientAsync();
            var seeded = await SeedPlanAsync(
                patient.Id,
                adherenceState: AdherenceState.NotAdhereInvalid);

            const string reason =
                "Patient completed the antibiotic course and was discharged in stable condition";
            var command = CreateCommand(seeded.Id, reason);

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            AssertSuccess(result);

            var saved = await LoadPlanAsync(seeded.Id);

            // Business rule: closing sets the final status and stores the reason
            Assert.Equal(TreatmentPlanStatus.Closed, saved.Status);
            Assert.Equal(reason, saved.CloseReason);

            // The row keeps its creation time but the update timestamp is refreshed
            Assert.Equal(SeedCreatedAt, saved.CreatedAt);
            Assert.True(saved.UpdatedAt > SeedUpdatedAt);

            // Closing only touches the status, close reason and update timestamp
            Assert.Equal(seeded.Id, saved.Id);
            Assert.Equal(seeded.PatientId, saved.PatientId);
            Assert.Equal(seeded.DoctorId, saved.DoctorId);
            Assert.Equal(seeded.DiagnosisId, saved.DiagnosisId);
            Assert.Equal(seeded.Diagnosis, saved.Diagnosis);
            Assert.Equal(seeded.Medicines, saved.Medicines);
            Assert.Equal(seeded.ReasonForMedicineChoice, saved.ReasonForMedicineChoice);
            Assert.Equal(seeded.AdherenceState, saved.AdherenceState);
            Assert.False(saved.IsDeleted);
        }

        [Fact]
        public async Task CloseTreatmentPlan_ReasonStoredVerbatim_Success()
        {
            // The close reason is free text from the doctor and must be persisted
            // exactly as submitted, including internal punctuation and spacing
            var patient = await SeedPatientAsync();
            var seeded = await SeedPlanAsync(patient.Id);

            const string reason =
                "Patient transferred to Bệnh viện Bạch Mai - ICU bed 12; family informed at 14:30";
            var command = CreateCommand(seeded.Id, reason);

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            AssertSuccess(result);

            var saved = await LoadPlanAsync(seeded.Id);
            Assert.Equal(TreatmentPlanStatus.Closed, saved.Status);
            Assert.Equal(reason, saved.CloseReason);
        }

        #endregion

        #region Fail path

        /*
         * Business rule: a closed plan is final. Closing it again must be rejected
         * and the original reason must not be overwritten.
         */
        [Fact]
        public async Task CloseTreatmentPlan_AlreadyClosedPlan_Fail()
        {
            const string originalReason =
                "Patient transferred to a provincial hospital";
            var patient = await SeedPatientAsync();
            var seeded = await SeedPlanAsync(
                patient.Id,
                status: TreatmentPlanStatus.Closed,
                closeReason: originalReason);

            var command = CreateCommand(
                seeded.Id,
                "Patient requested discharge against medical advice");

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            AssertFailure(result);

            var unchanged = await LoadPlanAsync(seeded.Id);
            Assert.Equal(TreatmentPlanStatus.Closed, unchanged.Status);
            Assert.Equal(originalReason, unchanged.CloseReason);
            Assert.Equal(SeedUpdatedAt, unchanged.UpdatedAt);
            Assert.Equal(seeded.DoctorId, unchanged.DoctorId);
            Assert.Equal(seeded.Diagnosis, unchanged.Diagnosis);
            Assert.Equal(seeded.Medicines, unchanged.Medicines);
            Assert.Equal(seeded.AdherenceState, unchanged.AdherenceState);
        }

        [Fact]
        public async Task CloseTreatmentPlan_UnknownPlan_Fail()
        {
            var command = CreateCommand(
                Guid.CreateVersion7(),
                "Patient completed the antibiotic course");

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            AssertFailure(result);
        }

        [Fact]
        public async Task CloseTreatmentPlan_SoftDeletedPlan_Fail()
        {
            // A soft-deleted plan is hidden by the query filter, so the lookup is
            // rejected exactly like an unknown ID and nothing is changed
            var patient = await SeedPatientAsync();
            var seeded = await SeedPlanAsync(
                patient.Id,
                softDeleted: true);

            var command = CreateCommand(
                seeded.Id,
                "Patient completed the antibiotic course");

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            AssertFailure(result);

            var unchanged = await LoadPlanIgnoringFiltersAsync(seeded.Id);
            Assert.Equal(TreatmentPlanStatus.InProgress, unchanged.Status);
            Assert.Null(unchanged.CloseReason);
            Assert.Equal(SeedUpdatedAt, unchanged.UpdatedAt);
            Assert.True(unchanged.IsDeleted);
        }

        #endregion
    }
}
