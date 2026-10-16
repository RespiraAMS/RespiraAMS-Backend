using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Application.Features.Patients.CreatePatient;
using Respira.Patient.Domain.Enums;
using Respira.Patient.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Test.Features.Patients.CreatePatient
{
    public class CreatePatientHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<PatientDbContext> _options;
        private readonly CreatePatientHandler _handler;
        private readonly IDbContext _context;

        public CreatePatientHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<PatientDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new PatientDbContext(_options);
            var mapper = new CreatePatientMapper();
            var logger = new Mock<ILogger<CreatePatientHandler>>().Object;

            // Initialize handler
            _handler = new(_context, mapper, logger);
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
        public async Task CreatePatient_ValidCommand_Success()
        {
            var command = ValidCommand();
            var before = DateTimeOffset.UtcNow;

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.NotEqual(Guid.Empty, result.Data.Id);
            var createdId = result.Data.Id;

            // Verify through a fresh context so the change tracker cannot mask a failed commit
            await using var freshContext = new PatientDbContext(_options);
            var saved = await freshContext.Patients.SingleAsync(
                x => x.Id == createdId, TestContext.Current.CancellationToken);
            Assert.Equal(command.FirstName, saved.FirstName);
            Assert.Equal(command.LastName, saved.LastName);
            Assert.Equal(command.PhoneNumber, saved.PhoneNumber);
            Assert.Equal(command.DateOfBirth, saved.DateOfBirth);
            Assert.Equal(command.Gender, saved.Gender);
            Assert.Equal(command.Height, saved.Height);
            Assert.Equal(command.Weight, saved.Weight);
            Assert.Equal(command.MedicalRecordCode, saved.MedicalRecordCode);
            Assert.Equal(command.HealthInsuranceCard, saved.HealthInsuranceCard);
            Assert.Equal(command.Address, saved.Address);
            Assert.Equal(command.City, saved.City);

            // Defaults owned by the mapper: admission is stamped during creation,
            // a brand-new patient is neither discharged nor treated yet
            Assert.InRange(saved.Admission, before.AddMilliseconds(-1), DateTimeOffset.UtcNow.AddMilliseconds(1));
            Assert.Null(saved.Discharge);
            Assert.Equal(TreatmentOutcome.InTreatment, saved.TreatmentOutcome);
            Assert.False(saved.IsDeleted);

            // Documented entity rules: age only counts years, full name is
            // "LastName FirstName" (Vietnamese style)
            Assert.Equal(DateTimeOffset.Now.Year - command.DateOfBirth.Year, saved.Age());
            Assert.Equal("Nguyễn Thị Mỹ Linh", saved.FullName);

            // Exactly one row was created
            Assert.Equal(1, await freshContext.Patients.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreatePatient_MessyNames_AreNormalized()
        {
            // The entity setters normalize names on assignment, so whatever the
            // admission desk types in, the stored record is cleaned up
            var command = ValidCommand() with
            {
                FirstName = "  mY   lINh ",
                LastName = " nguyễn   THỊ ",
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            var createdId = result.Data.Id;

            await using var freshContext = new PatientDbContext(_options);
            var saved = await freshContext.Patients.SingleAsync(
                x => x.Id == createdId, TestContext.Current.CancellationToken);
            Assert.Equal("My Linh", saved.FirstName);
            Assert.Equal("Nguyễn Thị", saved.LastName);
            Assert.Equal("Nguyễn Thị My Linh", saved.FullName);
        }

        // Boundary values of plausible human measurements: premature newborn to very
        // tall / severely obese adult. The database stores them with their exact
        // decimal value (no rounding, no clamping)
        [Theory]
        [InlineData(46.5, 2.4)]
        [InlineData(156.8, 48.9)]
        [InlineData(215.5, 183.4)]
        public async Task CreatePatient_BodyMeasurements_PersistedExactly(double height, double weight)
        {
            var command = ValidCommand() with
            {
                Height = (decimal)height,
                Weight = (decimal)weight,
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            var createdId = result.Data.Id;

            await using var freshContext = new PatientDbContext(_options);
            var saved = await freshContext.Patients.SingleAsync(
                x => x.Id == createdId, TestContext.Current.CancellationToken);
            Assert.Equal((decimal)height, saved.Height);
            Assert.Equal((decimal)weight, saved.Weight);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task CreatePatient_DuplicateMedicalRecordCode_Falure()
        {
            // Create a patient with the same medical record code
            var seeded = await SeedPatientAsync();

            // Create a new patient with different information, same medical record code
            var command = new CreatePatientCommand
            {
                FirstName = "Nguyen",
                LastName = "A",
                PhoneNumber = "0974372615",
                DateOfBirth = new DateOnly(1996, 7, 22),
                Gender = Gender.Male,
                Height = 188.8m,
                Weight = 90.9m,
                MedicalRecordCode = seeded.MedicalRecordCode,
                HealthInsuranceCard = "1234567890",
                Address = "18 Lý Thái Tổ, quận Hoàn Kiếm",
                City = "Hà Nội",
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        public static readonly TheoryData<DateOnly> InvalidAges = [
            new DateOnly(DateTimeOffset.UtcNow.Year - 10, 7, 22),
            // Boundary value: exact datetime
            new DateOnly(DateTimeOffset.UtcNow.Year - 15, DateTimeOffset.UtcNow.Month, DateTimeOffset.UtcNow.Day),
            // Boundary value: month doesn't matter when calculating age
            new DateOnly(
                DateTimeOffset.UtcNow.Year - 15,
                DateTimeOffset.UtcNow.Month >= 1 ? DateTimeOffset.UtcNow.Month - 1 : DateTimeOffset.UtcNow.Month,
                22),
            // Boundary value: day doesn't matter when calculating age
            new DateOnly(
                DateTimeOffset.UtcNow.Year - 15,
                DateTimeOffset.UtcNow.Month,
                DateTimeOffset.UtcNow.Day >= 1 ? DateTimeOffset.UtcNow.Day - 1 : DateTimeOffset.UtcNow.Day),
        ];

        [Theory]
        [MemberData(nameof(InvalidAges))]
        public async Task CreatePatient_Below16Years_Failure(DateOnly dob)
        {
            var command = new CreatePatientCommand
            {
                FirstName = "Nguyen",
                LastName = "A",
                PhoneNumber = "0974372615",
                DateOfBirth = dob,
                Gender = Gender.Male,
                Height = 188.8m,
                Weight = 90.9m,
                MedicalRecordCode = "BN-2026-004817",
                HealthInsuranceCard = "1234567890",
                Address = "18 Lý Thái Tổ, quận Hoàn Kiếm",
                City = "Hà Nội",
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);
            Assert.Null(result.Data);
        }

        #endregion

        private async Task<Domain.Entities.Patient> SeedPatientAsync()
        {
            // Committed through a separate context so the handler's change tracker
            // never sees the row and the conflict can only come from the database
            await using var context = new PatientDbContext(_options);
            var patient = new CreatePatientMapper().ToModel(ValidCommand()).Data!;
            await context.Patients.AddAsync(patient, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return patient;
        }

        private static CreatePatientCommand ValidCommand() => new()
        {
            FirstName = "Mỹ Linh",
            LastName = "Nguyễn Thị",
            PhoneNumber = "0984372615",
            DateOfBirth = new DateOnly(1996, 7, 22),
            Gender = Gender.Female,
            Height = 156.8m,
            Weight = 48.9m,
            MedicalRecordCode = "BN-2026-004817",
            HealthInsuranceCard = "8816372045",
            Address = "18 Lý Thái Tổ, quận Hoàn Kiếm",
            City = "Hà Nội",
        };
    }
}
