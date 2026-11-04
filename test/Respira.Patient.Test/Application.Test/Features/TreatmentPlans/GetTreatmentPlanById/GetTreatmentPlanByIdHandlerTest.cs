using Microsoft.EntityFrameworkCore;
using Respira.Patient.Application.Features.TreatmentPlans.GetTreatmentPlanById;
using Respira.Patient.Domain.Entities;
using Respira.Patient.Domain.Enums;
using Respira.Patient.Domain.Models;
using Respira.Patient.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Test.Features.TreatmentPlans.GetTreatmentPlanById
{
    public class GetTreatmentPlanByIdHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        // Fixed timestamps so the returned row can be proven to be the seeded one
        private static readonly DateTimeOffset SeedCreatedAt = new(2026, 1, 15, 8, 30, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset SeedUpdatedAt = new(2026, 1, 16, 9, 45, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset SeedDeletedAt = new(2026, 9, 30, 14, 5, 0, TimeSpan.Zero);

        private readonly DbContextOptions<PatientDbContext> _options;
        private readonly GetTreatmentPlanByIdHandler _handler;
        private readonly PatientDbContext _context;

        public GetTreatmentPlanByIdHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<PatientDbContext>()
                .UseNpgsql(fixture.ConnectionString).Options;
            _context = new PatientDbContext(_options);

            // Initialize handler
            _handler = new(_context);
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

        private async Task<Domain.Entities.Patient> SeedPatientAsync(
            string firstName = "Thanh Tùng",
            string lastName = "Võ",
            string medicalRecordCode = "BN-2026-004912")
        {
            // Committed through a separate context so the handler's change tracker
            // never sees the row and only the database can satisfy the lookup
            await using var context = new PatientDbContext(_options);

            var patient = new Domain.Entities.Patient
            {
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = "0903482715",
                DateOfBirth = new DateOnly(1968, 3, 17),
                Gender = Gender.Male,
                Height = 168.4m,
                Weight = 64.2m,
                MedicalRecordCode = medicalRecordCode,
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
            AdherenceState adherenceState = AdherenceState.NotAdhere,
            TreatmentPlanStatus status = TreatmentPlanStatus.InProgress,
            string? closeReason = null,
            List<Antibiotic>? medicines = null,
            string? reasonForMedicineChoice =
                "Blood culture grew Pseudomonas aeruginosa; escalated to meropenem and added vancomycin for suspected MRSA",
            string? diagnosis = null,
            Guid? doctorId = null,
            bool softDeleted = false)
        {
            var diagnosisId = Guid.CreateVersion7();
            var plan = new TreatmentPlan
            {
                PatientId = patientId,
                DoctorId = doctorId ?? Guid.CreateVersion7(),
                DiagnosisId = diagnosisId,
                Diagnosis = diagnosis ?? DiagnosisJson(diagnosisId),
                Medicines = medicines ??
                [
                    new Antibiotic(Guid.CreateVersion7(), "Meropenem", "Reserve"),
                    new Antibiotic(Guid.CreateVersion7(), "Azithromycin", "Watch"),
                    new Antibiotic(Guid.CreateVersion7(), "Vancomycin", "Watch"),
                ],
                ReasonForMedicineChoice = reasonForMedicineChoice,
                AdherenceState = adherenceState,
                Status = status,
                CloseReason = closeReason,
                CreatedAt = SeedCreatedAt,
                UpdatedAt = SeedUpdatedAt,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? SeedDeletedAt : null,
            };

            await using var context = new PatientDbContext(_options);
            context.TreatmentPlans.Add(plan);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return plan;
        }

        #endregion

        #region Query helpers

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

        private static GetTreatmentPlanByIdQuery CreateQuery(Guid id)
        {
            return new GetTreatmentPlanByIdQuery
            {
                Id = id,
            };
        }

        private static TreatmentPlanResult AssertSuccess(
            Result<TreatmentPlanResult> result)
        {
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);
            return result.Data;
        }

        private static void AssertNotFound(Result<TreatmentPlanResult> result)
        {
            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.Error.StatusCode);
        }

        #endregion

        #region Happy path

        /*
         * Business rule: the detail view exposes the plan with its patient identity,
         * doctor, raw diagnosis payload, prescribed medicines and current state.
         */
        [Fact]
        public async Task GetTreatmentPlanById_ExistingInProgressPlan_ReturnsAllFields()
        {
            var patient = await SeedPatientAsync();
            var seeded = await SeedPlanAsync(
                patient.Id,
                adherenceState: AdherenceState.NotAdhereInvalid);

            var query = CreateQuery(seeded.Id);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            var plan = AssertSuccess(result);

            Assert.Equal(seeded.Id, plan.Id);

            // The patient summary is projected from the patient row, and the name
            // follows the domain rule "{LastName} {FirstName}"
            Assert.NotNull(plan.Patient);
            Assert.Equal(patient.Id, plan.Patient.Id);
            Assert.Equal("Võ Thanh Tùng", plan.Patient.Name);

            Assert.Equal(seeded.DoctorId, plan.DoctorId);
            Assert.Equal(seeded.Diagnosis, plan.Diagnosis);
            Assert.Equal(seeded.Medicines, plan.Medicines);
            Assert.Equal(
                seeded.ReasonForMedicineChoice,
                plan.ReasonForMedicineChoice);
            Assert.Equal(AdherenceState.NotAdhereInvalid, plan.AdherenceState);
            Assert.Equal(TreatmentPlanStatus.InProgress, plan.Status);

            // An open plan has no close reason
            Assert.Null(plan.CloseReason);
        }

        // Business rule: the engine's adherence state must be surfaced unchanged for
        // every defined state, including the two doctor-decided ones
        [Theory]
        [InlineData(AdherenceState.Adhere)]
        [InlineData(AdherenceState.NotAdhere)]
        [InlineData(AdherenceState.NotAdhereValid)]
        [InlineData(AdherenceState.NotAdhereInvalid)]
        public async Task GetTreatmentPlanById_EachAdherenceState_ReturnsState(
            AdherenceState adherenceState)
        {
            var patient = await SeedPatientAsync();
            var seeded = await SeedPlanAsync(
                patient.Id,
                adherenceState: adherenceState);

            var result = await _handler.HandleAsync(
                CreateQuery(seeded.Id),
                TestContext.Current.CancellationToken);

            var plan = AssertSuccess(result);
            Assert.Equal(adherenceState, plan.AdherenceState);
        }

        [Fact]
        public async Task GetTreatmentPlanById_ClosedPlan_ReturnsCloseReason()
        {
            const string closeReason =
                "Patient transferred to Bệnh viện Bạch Mai - ICU bed 12; family informed at 14:30";

            var patient = await SeedPatientAsync();
            var seeded = await SeedPlanAsync(
                patient.Id,
                status: TreatmentPlanStatus.Closed,
                closeReason: closeReason);

            var result = await _handler.HandleAsync(
                CreateQuery(seeded.Id),
                TestContext.Current.CancellationToken);

            var plan = AssertSuccess(result);

            // Business rule: a closed plan keeps the reason recorded when it was closed
            Assert.Equal(TreatmentPlanStatus.Closed, plan.Status);
            Assert.Equal(closeReason, plan.CloseReason);
        }

        [Fact]
        public async Task GetTreatmentPlanById_MultiplePlansForPatient_ReturnsRequestedPlan()
        {
            var patient = await SeedPatientAsync();

            var olderPlan = await SeedPlanAsync(
                patient.Id,
                adherenceState: AdherenceState.Adhere,
                status: TreatmentPlanStatus.Closed,
                closeReason: "Completed the initial antibiotic course",
                diagnosis: "{\"summary\":\"Initial admission\"}");

            var requestedPlan = await SeedPlanAsync(
                patient.Id,
                adherenceState: AdherenceState.NotAdhere,
                diagnosis: "{\"summary\":\"Relapsed after discharge\"}");

            var newerPlan = await SeedPlanAsync(
                patient.Id,
                adherenceState: AdherenceState.NotAdhereValid,
                diagnosis: "{\"summary\":\"Follow-up review\"}");

            var result = await _handler.HandleAsync(
                CreateQuery(requestedPlan.Id),
                TestContext.Current.CancellationToken);

            var plan = AssertSuccess(result);

            // The lookup is by ID, so sibling plans must never leak into the result
            Assert.Equal(requestedPlan.Id, plan.Id);
            Assert.Equal(requestedPlan.Diagnosis, plan.Diagnosis);
            Assert.Equal(AdherenceState.NotAdhere, plan.AdherenceState);
            Assert.Equal(TreatmentPlanStatus.InProgress, plan.Status);
            Assert.NotEqual(olderPlan.Id, plan.Id);
            Assert.NotEqual(newerPlan.Id, plan.Id);

            // The patient summary is still resolved for the requested plan
            Assert.Equal(patient.Id, plan.Patient.Id);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task GetTreatmentPlanById_UnknownPlan_ReturnsResourceNotFound()
        {
            var result = await _handler.HandleAsync(
                CreateQuery(Guid.CreateVersion7()),
                TestContext.Current.CancellationToken);

            AssertNotFound(result);
        }

        // Boundary: Guid.Empty is the lowest possible ID and cannot identify a row
        [Fact]
        public async Task GetTreatmentPlanById_EmptyPlanId_ReturnsResourceNotFound()
        {
            var result = await _handler.HandleAsync(
                CreateQuery(Guid.Empty),
                TestContext.Current.CancellationToken);

            AssertNotFound(result);
        }

        [Fact]
        public async Task GetTreatmentPlanById_SoftDeletedPlan_ReturnsResourceNotFound()
        {
            // A soft-deleted plan is hidden by the query filter, so it must be
            // reported exactly like an unknown ID
            var patient = await SeedPatientAsync();
            var seeded = await SeedPlanAsync(
                patient.Id,
                softDeleted: true);

            var result = await _handler.HandleAsync(
                CreateQuery(seeded.Id),
                TestContext.Current.CancellationToken);

            AssertNotFound(result);
        }

        #endregion
    }
}
