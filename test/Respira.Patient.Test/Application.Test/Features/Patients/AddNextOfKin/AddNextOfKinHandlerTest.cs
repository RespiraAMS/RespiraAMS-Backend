using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Application.Features.Patients.AddNextOfKin;
using Respira.Patient.Domain.Enums;
using Respira.Patient.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Test.Features.Patients.AddNextOfKin
{
    public class AddNextOfKinHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        // Fixed timestamps so "exactly equals" / soft-delete states can be seeded
        // deterministically
        private static readonly DateTimeOffset SeedAdmission = new(2026, 1, 15, 8, 30, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset SeedDeletedAt = new(2026, 9, 30, 14, 5, 0, TimeSpan.Zero);

        private readonly DbContextOptions<PatientDbContext> _options;
        private readonly AddNextOfKinHandler _handler;
        private readonly IDbContext _context;

        public AddNextOfKinHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<PatientDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new PatientDbContext(_options);
            var mapper = new AddNextOfKinMapper();
            var logger = new Mock<ILogger<AddNextOfKinHandler>>().Object;

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

        /*
         * Seeds three patients: the main one already has a registered next of kin
         * (the uniqueness guard runs against that list), a second live patient
         * without any kin, and a soft-deleted one that the query filter hides
         */
        private async Task<(
            Domain.Entities.Patient Main,
            Domain.Entities.Patient Other,
            Domain.Entities.Patient Removed,
            Domain.Entities.NextOfKin ExistingKin)> SeedAsync()
        {
            // Committed through a separate context so the handler's change tracker
            // never sees the rows: the returned entities stay pristine snapshots of
            // the seeded values for later assertions
            await using var context = new PatientDbContext(_options);

            var main = new Domain.Entities.Patient
            {
                FirstName = "Thanh Tuấn",
                LastName = "Võ",
                PhoneNumber = "0903482715",
                DateOfBirth = new DateOnly(1990, 4, 12),
                Gender = Gender.Male,
                Height = 171.2m,
                Weight = 68.7m,
                MedicalRecordCode = "BN-2026-004912",
                HealthInsuranceCard = "7016382471",
                Address = "45 Hai Bà Trưng, quận Hoàn Kiếm",
                City = "Hà Nội",
                Admission = SeedAdmission,
                TreatmentOutcome = TreatmentOutcome.InTreatment,
            };
            var other = new Domain.Entities.Patient
            {
                FirstName = "Hoài An",
                LastName = "Trịnh Thị",
                PhoneNumber = "0987164530",
                DateOfBirth = new DateOnly(1994, 9, 27),
                Gender = Gender.Female,
                Height = 158.6m,
                Weight = 51.3m,
                MedicalRecordCode = "BN-2026-005376",
                HealthInsuranceCard = "8816372058",
                Address = "12 Trần Phú, quận Ninh Kiều",
                City = "Cần Thơ",
                Admission = SeedAdmission,
                TreatmentOutcome = TreatmentOutcome.InTreatment,
            };
            var removed = new Domain.Entities.Patient
            {
                FirstName = "Hồng",
                LastName = "Vũ Thị",
                PhoneNumber = "0907835412",
                DateOfBirth = new DateOnly(1976, 11, 2),
                Gender = Gender.Female,
                Height = 154.7m,
                Weight = 57.8m,
                MedicalRecordCode = "BN-2026-007315",
                HealthInsuranceCard = "8816372059",
                Address = "18 Lý Thái Tổ, quận Hoàn Kiếm",
                City = "Hà Nội",
                Admission = SeedAdmission,
                TreatmentOutcome = TreatmentOutcome.InTreatment,
                IsDeleted = true,
                DeletedAt = SeedDeletedAt,
            };
            await context.Patients.AddRangeAsync(
                [main, other, removed], TestContext.Current.CancellationToken);

            var existingKin = new Domain.Entities.NextOfKin
            {
                PatientId = main.Id,
                FullName = "Nguyễn Thị Mai Anh",
                PhoneNumber = "0987654321",
                Relationship = "Mẹ",
            };
            await context.NextOfKins.AddAsync(existingKin, TestContext.Current.CancellationToken);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return (main, other, removed, existingKin);
        }

        #region Happy path

        [Fact]
        public async Task AddNextOfKin_ValidKin_Created_Success()
        {
            var (main, _, _, _) = await SeedAsync();
            var command = new AddNextOfKinCommand
            {
                PatientId = main.Id,
                // Lower-case and untrimmed input: the entity normalizes on assignment
                FullName = "trần đức minh",
                PhoneNumber = "0913458769",
                Relationship = "Con trai",
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.NotEqual(Guid.Empty, result.Data.Id);

            // The new kin is persisted with every value of the command - the name
            // goes through the entity normalizer, phone and relationship verbatim -
            // and joins the patient's existing next of kin (2 in total)
            await using var verify = new PatientDbContext(_options);
            var newId = result.Data.Id;
            var saved = await verify.NextOfKins.SingleAsync(
                x => x.Id == newId, TestContext.Current.CancellationToken);
            Assert.Equal(main.Id, saved.PatientId);
            Assert.Equal("Trần Đức Minh", saved.FullName);
            Assert.Equal(command.PhoneNumber, saved.PhoneNumber);
            Assert.Equal(command.Relationship, saved.Relationship);
            Assert.False(saved.IsDeleted);
            Assert.Equal(2, await verify.NextOfKins.CountAsync(
                x => x.PatientId == main.Id, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AddNextOfKin_SamePhoneNumberOnDifferentPatient_Created_Success()
        {
            /*
             * Business rule (handler note): the unique check is scoped to a single
             * patient - "a same person can be next of kin of multiple patients".
             * The same grandmother already registered for one patient must still be
             * registrable for another one
             */
            var (_, other, _, existingKin) = await SeedAsync();
            var command = new AddNextOfKinCommand
            {
                PatientId = other.Id,
                FullName = existingKin.FullName,
                PhoneNumber = existingKin.PhoneNumber,
                Relationship = existingKin.Relationship,
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.NotEqual(Guid.Empty, result.Data.Id);

            await using var verify = new PatientDbContext(_options);
            var newId = result.Data.Id;
            var saved = await verify.NextOfKins.SingleAsync(
                x => x.Id == newId, TestContext.Current.CancellationToken);
            Assert.Equal(other.Id, saved.PatientId);
            Assert.Equal(existingKin.PhoneNumber, saved.PhoneNumber);
        }

        [Fact]
        public async Task AddNextOfKin_DuplicateRelationship_Created_Success()
        {
            /*
             * Business rule (handler comment): relationship is deliberately not
             * unique - "sibling for example, you can have many" - so two elder
             * brothers of the same patient are both accepted
             */
            var (main, _, _, _) = await SeedAsync();
            var firstCommand = new AddNextOfKinCommand
            {
                PatientId = main.Id,
                FullName = "Võ Quốc Huy",
                PhoneNumber = "0376928415",
                Relationship = "Anh trai",
            };
            var secondCommand = new AddNextOfKinCommand
            {
                PatientId = main.Id,
                FullName = "Võ Minh Đạt",
                PhoneNumber = "0905174823",
                Relationship = "Anh trai",
            };

            var first = await _handler.HandleAsync(firstCommand, TestContext.Current.CancellationToken);
            Assert.True(first.IsSuccess());
            Assert.Null(first.Error);
            Assert.Equal(ApplicationStatus.Created, first.StatusCode);
            Assert.NotNull(first.Data);

            var second = await _handler.HandleAsync(secondCommand, TestContext.Current.CancellationToken);
            Assert.True(second.IsSuccess());
            Assert.Null(second.Error);
            Assert.Equal(ApplicationStatus.Created, second.StatusCode);
            Assert.NotNull(second.Data);

            await using var verify = new PatientDbContext(_options);
            Assert.Equal(3, await verify.NextOfKins.CountAsync(
                x => x.PatientId == main.Id, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AddNextOfKin_DuplicateFullNameDifferentPhoneNumber_Created_Success()
        {
            /*
             * Business rule (handler comment): full name "is not really safe to
             * judge" and is never checked for uniqueness - a daughter traditionally
             * carrying her mother's name is a different person and passes with her
             * own phone number
             */
            var (main, _, _, existingKin) = await SeedAsync();
            var command = new AddNextOfKinCommand
            {
                PatientId = main.Id,
                FullName = existingKin.FullName, // same name as the already registered kin
                PhoneNumber = "0907164582",
                Relationship = "Con gái",
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);

            await using var verify = new PatientDbContext(_options);
            var newId = result.Data.Id;
            var saved = await verify.NextOfKins.SingleAsync(
                x => x.Id == newId, TestContext.Current.CancellationToken);
            Assert.Equal(existingKin.FullName, saved.FullName);
            Assert.Equal(command.PhoneNumber, saved.PhoneNumber);
            Assert.Equal(2, await verify.NextOfKins.CountAsync(
                x => x.PatientId == main.Id, TestContext.Current.CancellationToken));
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task AddNextOfKin_UnknownPatient_Fail()
        {
            var command = new AddNextOfKinCommand
            {
                PatientId = Guid.CreateVersion7(),
                FullName = "Lâm Quang Vinh",
                PhoneNumber = "0932581746",
                Relationship = "Cha",
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Nothing is written when the patient does not exist
            await using var verify = new PatientDbContext(_options);
            Assert.Equal(0, await verify.NextOfKins.CountAsync(
                TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AddNextOfKin_SoftDeletedPatient_Fail()
        {
            // A soft-deleted patient is hidden by the query filter, so the lookup is
            // rejected exactly like an unknown ID
            var (_, _, removed, _) = await SeedAsync();
            var command = new AddNextOfKinCommand
            {
                PatientId = removed.Id,
                FullName = "Phạm Ngọc Lan",
                PhoneNumber = "0971648253",
                Relationship = "Vợ",
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var verify = new PatientDbContext(_options);
            Assert.Equal(0, await verify.NextOfKins.CountAsync(
                x => x.PatientId == removed.Id, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AddNextOfKin_DuplicatePhoneNumberSamePatient_Fail()
        {
            /*
             * Business rule (handler comment): the phone number is the only unique
             * attribute, and the check runs against the same patient - re-submitting
             * the same person is rejected and nothing is written
             */
            var (main, _, _, existingKin) = await SeedAsync();
            var command = new AddNextOfKinCommand
            {
                PatientId = main.Id,
                FullName = existingKin.FullName,
                PhoneNumber = existingKin.PhoneNumber, // already registered for this patient
                Relationship = existingKin.Relationship,
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var verify = new PatientDbContext(_options);
            Assert.Equal(1, await verify.NextOfKins.CountAsync(
                x => x.PatientId == main.Id, TestContext.Current.CancellationToken));
        }

        #endregion
    }
}
