using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Application.Features.Patients.UpdatePatient;
using Respira.Patient.Domain.Enums;
using Respira.Patient.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Test.Features.Patients.UpdatePatient
{
    public class UpdatePatientHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        // Fixed timestamps so "must not change" assertions can be exact
        private static readonly DateTimeOffset SeedAdmission = new(2026, 1, 15, 8, 30, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset SeedDischarge = new(2026, 2, 3, 10, 15, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset SeedUpdatedAt = new(2026, 1, 15, 8, 45, 0, TimeSpan.Zero);

        private readonly DbContextOptions<PatientDbContext> _options;
        private readonly UpdatePatientHandler _handler;
        private readonly IDbContext _context;

        public UpdatePatientHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<PatientDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new PatientDbContext(_options);
            var logger = new Mock<ILogger<UpdatePatientHandler>>().Object;

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

        [Fact]
        public async Task UpdatePatient_WithoutTreatment_UpdatesAllMutableFields_Success()
        {
            var seeded = await SeedPatientAsync();
            var command = ValidCommand(seeded.Id);
            var before = DateTimeOffset.UtcNow;

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            // Verify through a fresh context so the change tracker cannot mask a failed commit
            await using var freshContext = new PatientDbContext(_options);
            var saved = await freshContext.Patients.SingleAsync(
                x => x.Id == seeded.Id, TestContext.Current.CancellationToken);

            // Rule 4: free fields change on every update, names are normalized by
            // the entity setters on the way in
            Assert.Equal("Minh Quân", saved.FirstName);
            Assert.Equal("Trần Hoàng Minh", saved.LastName);
            Assert.Equal(command.PhoneNumber, saved.PhoneNumber);
            Assert.Equal(command.MedicalRecordCode, saved.MedicalRecordCode);
            Assert.Equal(command.HealthInsuranceCard, saved.HealthInsuranceCard);
            Assert.Equal(command.Address, saved.Address);
            Assert.Equal(command.City, saved.City);

            // Rule 3: clinical information applies because the patient has no treatment
            Assert.Equal(command.Gender, saved.Gender);
            Assert.Equal(command.Height, saved.Height);
            Assert.Equal(command.Weight, saved.Weight);

            // Rule 1: admission must not be changed in all cases
            Assert.Equal(SeedAdmission, saved.Admission);
            // Rule 2: discharge and treatment outcome are not this handler's business
            Assert.Null(saved.Discharge);
            Assert.Equal(TreatmentOutcome.InTreatment, saved.TreatmentOutcome);

            // The amendment is auditable
            Assert.InRange(saved.UpdatedAt, before.AddMilliseconds(-1), DateTimeOffset.UtcNow.AddMilliseconds(1));
            Assert.False(saved.IsDeleted);
        }

        /*
         * Business rule (handler doc, point 3): date of birth is clinical
         * information, updated when the patient has no treatment - and the new value
         * is exactly the one the age rule validated before committing
         */
        [Fact]
        public async Task UpdatePatient_WithoutTreatment_NewDateOfBirthPersisted()
        {
            var seeded = await SeedPatientAsync();
            var command = ValidCommand(seeded.Id);

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());

            await using var freshContext = new PatientDbContext(_options);
            var saved = await freshContext.Patients.SingleAsync(
                x => x.Id == seeded.Id, TestContext.Current.CancellationToken);

            Assert.Equal(command.DateOfBirth, saved.DateOfBirth);
            // The stored age follows the stored date of birth (year-only calculation)
            Assert.Equal(DateTimeOffset.Now.Year - command.DateOfBirth.Year, saved.Age());
        }

        // Boundary of the age rule (>= 16): exactly 16 is the lowest accepted age,
        // and the year-only calculation makes the month and day of the birthday
        // irrelevant - Jan 1 and Dec 31 of that year are accepted all the same
        public static readonly TheoryData<DateOnly> ValidAges =
        [
            new DateOnly(DateTimeOffset.UtcNow.Year - 16, 1, 1),
            new DateOnly(DateTimeOffset.UtcNow.Year - 16, DateTimeOffset.UtcNow.Month, DateTimeOffset.UtcNow.Day),
            new DateOnly(DateTimeOffset.UtcNow.Year - 16, 12, 31),
        ];

        [Theory]
        [MemberData(nameof(ValidAges))]
        public async Task UpdatePatient_AgeAtBoundary16_UpdatesClinicalInformation_Success(DateOnly dob)
        {
            var seeded = await SeedPatientAsync();
            var command = ValidCommand(seeded.Id) with { DateOfBirth = dob };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            // The clinical branch was taken, so the age rule accepted the boundary
            await using var freshContext = new PatientDbContext(_options);
            var saved = await freshContext.Patients.SingleAsync(
                x => x.Id == seeded.Id, TestContext.Current.CancellationToken);
            Assert.Equal(command.Gender, saved.Gender);
            Assert.Equal(command.Height, saved.Height);
            Assert.Equal(command.Weight, saved.Weight);
        }

        [Fact]
        public async Task UpdatePatient_WithTreatment_KeepsClinicalInformationLocked()
        {
            /*
             * Business rule (handler doc, point 3): clinical information can only be
             * updated while the patient has no treatment, so gender, height, weight
             * and date of birth stay at their stored values even though the command
             * carries brand-new ones
             */
            var seeded = await SeedPatientAsync(withTreatment: true);
            var command = ValidCommand(seeded.Id);

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new PatientDbContext(_options);
            var saved = await freshContext.Patients.SingleAsync(
                x => x.Id == seeded.Id, TestContext.Current.CancellationToken);

            Assert.Equal(seeded.Gender, saved.Gender);
            Assert.Equal(seeded.Height, saved.Height);
            Assert.Equal(seeded.Weight, saved.Weight);
            Assert.Equal(seeded.DateOfBirth, saved.DateOfBirth);

            // The free fields still update (rule 4)
            Assert.Equal(command.PhoneNumber, saved.PhoneNumber);
            Assert.Equal(command.Address, saved.Address);
            Assert.Equal(command.City, saved.City);
            Assert.Equal(SeedAdmission, saved.Admission);
        }

        [Fact]
        public async Task UpdatePatient_DischargedPatient_DischargeAndOutcomeUntouched()
        {
            /*
             * Business rule (handler doc, point 2): discharge and treatment outcome
             * may change over the life of a patient, but not through this handler -
             * and rule 1 keeps the admission timestamp immutable as well
             */
            var seeded = await SeedPatientAsync(withTreatment: true, discharged: true);
            var command = ValidCommand(seeded.Id);

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new PatientDbContext(_options);
            var saved = await freshContext.Patients.SingleAsync(
                x => x.Id == seeded.Id, TestContext.Current.CancellationToken);

            Assert.Equal(SeedDischarge, saved.Discharge);
            Assert.Equal(TreatmentOutcome.Recovered, saved.TreatmentOutcome);
            Assert.Equal(SeedAdmission, saved.Admission);
            Assert.Equal(command.PhoneNumber, saved.PhoneNumber);
        }

        [Fact]
        public async Task UpdatePatient_UpdatesOwnMedicalRecordCode_Success()
        {
            /*
             * The uniqueness rule only rejects a code held by ANOTHER live patient:
             * keeping (or re-entering) the patient's own code must not block the
             * amendment
             */
            var seeded = await SeedPatientAsync();
            var command = ValidCommand(seeded.Id) with
            {
                MedicalRecordCode = seeded.MedicalRecordCode,
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new PatientDbContext(_options);
            var saved = await freshContext.Patients.SingleAsync(
                x => x.Id == seeded.Id, TestContext.Current.CancellationToken);
            Assert.Equal(seeded.MedicalRecordCode, saved.MedicalRecordCode);
            Assert.Equal(command.PhoneNumber, saved.PhoneNumber);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task UpdatePatient_UnknownId_Fail()
        {
            var command = ValidCommand(Guid.CreateVersion7());

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Nothing may be created when the target does not exist
            await using var freshContext = new PatientDbContext(_options);
            Assert.Equal(0, await freshContext.Patients.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task UpdatePatient_SoftDeletedPatient_Fail()
        {
            // A soft-deleted patient is hidden by the query filter, so the update
            // must be rejected exactly like an unknown ID and change nothing
            var seeded = await SeedPatientAsync(softDeleted: true);
            var command = ValidCommand(seeded.Id);

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new PatientDbContext(_options);
            var rows = await freshContext.Patients.IgnoreQueryFilters()
                .ToListAsync(TestContext.Current.CancellationToken);
            var row = Assert.Single(rows);
            Assert.Equal(seeded.Id, row.Id);
            Assert.Equal(seeded.FirstName, row.FirstName);
            Assert.Equal(seeded.PhoneNumber, row.PhoneNumber);
            Assert.Equal(seeded.UpdatedAt, row.UpdatedAt);
        }

        [Fact]
        public async Task UpdatePatient_DuplicateMedicalRecordCode_Fail()
        {
            // Another live patient already holds the code this update wants to claim
            var seeded = await SeedPatientAsync();
            var other = await SeedPatientAsync(medicalRecordCode: "BN-2026-006044");

            var command = ValidCommand(seeded.Id) with
            {
                MedicalRecordCode = other.MedicalRecordCode,
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Neither patient may change: the conflict is detected before any write
            await using var freshContext = new PatientDbContext(_options);
            var rows = await freshContext.Patients.IgnoreQueryFilters()
                .OrderBy(x => x.MedicalRecordCode)
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal(2, rows.Count);
            var target = rows.Single(x => x.Id == seeded.Id);
            Assert.Equal(seeded.FirstName, target.FirstName);
            Assert.Equal(seeded.MedicalRecordCode, target.MedicalRecordCode);
            Assert.Equal(seeded.UpdatedAt, target.UpdatedAt);
            var holder = rows.Single(x => x.Id == other.Id);
            Assert.Equal(other.UpdatedAt, holder.UpdatedAt);
        }

        // Boundary of the age rule (>= 16): 15 is the last rejected year - the
        // year-only calculation makes month and day irrelevant, so every date of
        // that year is rejected, and a 10 year old child is rejected outright
        public static readonly TheoryData<DateOnly> InvalidAges =
        [
            new DateOnly(DateTimeOffset.UtcNow.Year - 10, 7, 22),
            new DateOnly(DateTimeOffset.UtcNow.Year - 15, DateTimeOffset.UtcNow.Month, DateTimeOffset.UtcNow.Day),
            new DateOnly(DateTimeOffset.UtcNow.Year - 15, 12, 31),
        ];

        [Theory]
        [MemberData(nameof(InvalidAges))]
        public async Task UpdatePatient_DateOfBirthBelowAge16_Fail(DateOnly dob)
        {
            var seeded = await SeedPatientAsync();
            var command = ValidCommand(seeded.Id) with { DateOfBirth = dob };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);

            /*
             * Patient should stay the same, no field should be updated 
             */
            await using var freshContext = new PatientDbContext(_options);
            var saved = await freshContext.Patients.SingleAsync(
                x => x.Id == seeded.Id, TestContext.Current.CancellationToken);
            Assert.Equal(seeded.FirstName, saved.FirstName);
            Assert.Equal(seeded.PhoneNumber, saved.PhoneNumber);
            Assert.Equal(seeded.MedicalRecordCode, saved.MedicalRecordCode);
            Assert.Equal(seeded.HealthInsuranceCard, saved.HealthInsuranceCard);
            Assert.Equal(seeded.Address, saved.Address);
            Assert.Equal(seeded.City, saved.City);
            Assert.Equal(seeded.UpdatedAt, saved.UpdatedAt);
            Assert.Equal(seeded.DateOfBirth, saved.DateOfBirth);
            Assert.Equal(seeded.Gender, saved.Gender);
            Assert.Equal(seeded.Height, saved.Height);
            Assert.Equal(seeded.Weight, saved.Weight);
        }

        #endregion

        private async Task<Domain.Entities.Patient> SeedPatientAsync(
            string medicalRecordCode = "BN-2026-004817",
            bool withTreatment = false,
            bool softDeleted = false,
            bool discharged = false)
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
                Discharge = discharged ? SeedDischarge : null,
                TreatmentOutcome = discharged ? TreatmentOutcome.Recovered : TreatmentOutcome.InTreatment,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? SeedDischarge : null,
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

        private static UpdatePatientCommand ValidCommand(Guid id) => new()
        {
            Id = id,
            FirstName = " Minh   QUÂN ",
            LastName = "trần hoàng  MINH",
            PhoneNumber = "0976251843",
            DateOfBirth = new DateOnly(1991, 11, 9),
            Gender = Gender.Female,
            Height = 158.2m,
            Weight = 51.7m,
            MedicalRecordCode = "BN-2026-005291",
            HealthInsuranceCard = "7016382459",
            Address = "18 Lý Thái Tổ, quận Hoàn Kiếm",
            City = "Hà Nội",
        };
    }
}
