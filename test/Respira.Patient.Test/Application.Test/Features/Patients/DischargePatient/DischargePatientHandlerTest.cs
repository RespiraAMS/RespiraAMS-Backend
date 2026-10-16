using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Application.Features.Patients.DischargePatient;
using Respira.Patient.Domain.Enums;
using Respira.Patient.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Test.Features.Patients.DischargePatient
{
    public class DischargePatientHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        // Fixed timestamps so "must not change" assertions can be exact
        private static readonly DateTimeOffset SeedAdmission = new(2026, 1, 15, 8, 30, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset SeedUpdatedAt = new(2026, 1, 15, 8, 45, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset SeedDeletedAt = new(2026, 9, 30, 14, 5, 0, TimeSpan.Zero);

        private readonly DbContextOptions<PatientDbContext> _options;
        private readonly DischargePatientHandler _handler;
        private readonly IDbContext _context;

        public DischargePatientHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<PatientDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new PatientDbContext(_options);
            var logger = new Mock<ILogger<DischargePatientHandler>>().Object;

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
            await _context.Treatments.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.NextOfKins.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Patients.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        #region Happy path

        // Business rule (handler doc): a patient is discharged only after at least
        // one treatment, and every TreatmentOutcome member - explicitly including
        // OutOfScope, which the doc legitimizes as a discharge in this system -
        // must be storable as the episode's outcome
        [Theory]
        [InlineData(TreatmentOutcome.InTreatment)]
        [InlineData(TreatmentOutcome.Recovered)]
        [InlineData(TreatmentOutcome.Death)]
        [InlineData(TreatmentOutcome.OutOfScope)]
        public async Task DischargePatient_WithTreatment_SetsDischargeAndOutcome_Success(TreatmentOutcome outcome)
        {
            var seeded = await SeedPatientAsync(withTreatment: true);
            var command = new DischargePatientCommand { Id = seeded.Id, Outcome = outcome };
            var before = DateTimeOffset.UtcNow;

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            // Verify through a fresh context so the change tracker cannot mask a failed commit
            await using var freshContext = new PatientDbContext(_options);
            var saved = await freshContext.Patients.SingleAsync(
                x => x.Id == seeded.Id, TestContext.Current.CancellationToken);

            // The discharge is stamped at discharge time (the record stays visible -
            // discharge is not a delete)
            Assert.NotNull(saved.Discharge);
            Assert.InRange(
                saved.Discharge.Value,
                before.AddMilliseconds(-1),
                DateTimeOffset.UtcNow.AddMilliseconds(1));
            Assert.Equal(command.Outcome, saved.TreatmentOutcome);

            // The amendment is auditable
            Assert.InRange(
                saved.UpdatedAt,
                before.AddMilliseconds(-1),
                DateTimeOffset.UtcNow.AddMilliseconds(1));

            // Discharge only stamps the discharge fields - the rest of the record
            // is left exactly as admitted
            Assert.Equal(SeedAdmission, saved.Admission);
            Assert.Equal(seeded.FirstName, saved.FirstName);
            Assert.Equal(seeded.LastName, saved.LastName);
            Assert.Equal(seeded.PhoneNumber, saved.PhoneNumber);
            Assert.Equal(seeded.DateOfBirth, saved.DateOfBirth);
            Assert.Equal(seeded.Gender, saved.Gender);
            Assert.Equal(seeded.Height, saved.Height);
            Assert.Equal(seeded.Weight, saved.Weight);
            Assert.Equal(seeded.MedicalRecordCode, saved.MedicalRecordCode);
            Assert.Equal(seeded.HealthInsuranceCard, saved.HealthInsuranceCard);
            Assert.False(saved.IsDeleted);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task DischargePatient_UnknownId_Fail()
        {
            var command = new DischargePatientCommand
            {
                Id = Guid.CreateVersion7(),
                Outcome = TreatmentOutcome.Recovered,
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Nothing may exist after discharging something that never did
            await using var freshContext = new PatientDbContext(_options);
            Assert.Equal(0, await freshContext.Patients.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DischargePatient_SoftDeletedPatient_Fail()
        {
            // A soft-deleted patient is hidden by the query filter, so the discharge
            // is rejected exactly like an unknown ID and changes nothing
            var seeded = await SeedPatientAsync(withTreatment: true, softDeleted: true);
            var command = new DischargePatientCommand
            {
                Id = seeded.Id,
                Outcome = TreatmentOutcome.Recovered,
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new PatientDbContext(_options);
            var row = await freshContext.Patients.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == seeded.Id, TestContext.Current.CancellationToken);
            Assert.True(row.IsDeleted);
            Assert.Equal(SeedDeletedAt, row.DeletedAt);
            Assert.Null(row.Discharge);
            Assert.Equal(TreatmentOutcome.InTreatment, row.TreatmentOutcome);
        }

        [Fact]
        public async Task DischargePatient_WithoutTreatment_Fail()
        {
            /*
             * Business rule (handler doc): "Only discharge patient if they have
             * receive at least one treatment" - a patient who was never treated
             * cannot be discharged, and the rejected attempt writes nothing: no
             * discharge stamp, no outcome change, no audit timestamp
             */
            var seeded = await SeedPatientAsync();
            var command = new DischargePatientCommand
            {
                Id = seeded.Id,
                Outcome = TreatmentOutcome.Recovered,
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new PatientDbContext(_options);
            var saved = await freshContext.Patients.SingleAsync(
                x => x.Id == seeded.Id, TestContext.Current.CancellationToken);
            Assert.Null(saved.Discharge);
            Assert.Equal(TreatmentOutcome.InTreatment, saved.TreatmentOutcome);
            Assert.Equal(SeedUpdatedAt, saved.UpdatedAt);
            Assert.Equal(seeded.FirstName, saved.FirstName);
            Assert.Equal(seeded.MedicalRecordCode, saved.MedicalRecordCode);
        }

        #endregion

        private async Task<Domain.Entities.Patient> SeedPatientAsync(
            string medicalRecordCode = "BN-2026-004817",
            bool withTreatment = false,
            bool softDeleted = false)
        {
            // Committed through a separate context so the handler's change tracker
            // never sees the row: the returned entity stays a pristine snapshot of
            // the seeded values for later assertions
            await using var context = new PatientDbContext(_options);
            var patient = new Domain.Entities.Patient
            {
                FirstName = "Quân",
                LastName = "Trần Hoàng Minh",
                PhoneNumber = "0912348765",
                DateOfBirth = new DateOnly(1988, 3, 14),
                Gender = Gender.Male,
                Height = 172.5m,
                Weight = 68.4m,
                MedicalRecordCode = medicalRecordCode,
                HealthInsuranceCard = "8816372045",
                Address = "27 Nguyễn Huệ, phường Bến Nghé",
                City = "TP. Hồ Chí Minh",
                Admission = SeedAdmission,
                TreatmentOutcome = TreatmentOutcome.InTreatment,
                IsDeleted = softDeleted,
                Discharge = null,
                DeletedAt = softDeleted ? SeedDeletedAt : null,
                UpdatedAt = SeedUpdatedAt,
            };
            await context.Patients.AddAsync(patient, TestContext.Current.CancellationToken);

            if (withTreatment)
            {
                await context.Treatments.AddAsync(new Domain.Entities.Treatment
                {
                    PatientId = patient.Id,
                }, TestContext.Current.CancellationToken);
            }

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return patient;
        }
    }
}
