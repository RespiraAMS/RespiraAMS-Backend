using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Application.Features.Patients.UpdateNextOfKin;
using Respira.Patient.Domain.Enums;
using Respira.Patient.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Test.Features.Patients.UpdateNextOfKin
{
    public class UpdateNextOfKinHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        // Fixed timestamps so "exactly equals" / soft-delete states can be seeded
        // deterministically
        private static readonly DateTimeOffset SeedAdmission = new(2026, 1, 15, 8, 30, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset SeedDeletedAt = new(2026, 9, 30, 14, 5, 0, TimeSpan.Zero);

        private readonly DbContextOptions<PatientDbContext> _options;
        private readonly UpdateNextOfKinHandler _handler;
        private readonly IDbContext _context;

        public UpdateNextOfKinHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<PatientDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new PatientDbContext(_options);
            var mapper = new UpdateNextOfKinMapper();
            var logger = new Mock<ILogger<UpdateNextOfKinHandler>>().Object;

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
         * Seeds two patients: the main one has two visible next of kin (the
         * uniqueness guard compares against the sibling) plus a soft-deleted one
         * that the query filter hides; the second patient holds a kin whose phone
         * number belongs to a different patient's scope
         */
        private async Task<(
            Domain.Entities.Patient Main,
            Domain.Entities.Patient Other,
            Domain.Entities.NextOfKin KinA,
            Domain.Entities.NextOfKin KinB,
            Domain.Entities.NextOfKin KinOfOther,
            Domain.Entities.NextOfKin RemovedKin)> SeedAsync()
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
            await context.Patients.AddRangeAsync([main, other], TestContext.Current.CancellationToken);

            // The kin are recorded at admission: the whole-second timestamp makes
            // the exact round-trip through the database deterministic
            var kinA = new Domain.Entities.NextOfKin
            {
                PatientId = main.Id,
                FullName = "Nguyễn Thị Mai Anh",
                PhoneNumber = "0987654321",
                Relationship = "Mẹ",
                CreatedAt = SeedAdmission,
            };
            var kinB = new Domain.Entities.NextOfKin
            {
                PatientId = main.Id,
                FullName = "Võ Quốc Huy",
                PhoneNumber = "0376928415",
                Relationship = "Anh trai",
                CreatedAt = SeedAdmission,
            };
            var removedKin = new Domain.Entities.NextOfKin
            {
                PatientId = main.Id,
                FullName = "Đỗ Thanh Tùng",
                PhoneNumber = "0904518276",
                Relationship = "Con trai",
                CreatedAt = SeedAdmission,
                IsDeleted = true,
                DeletedAt = SeedDeletedAt,
            };
            var kinOfOther = new Domain.Entities.NextOfKin
            {
                PatientId = other.Id,
                FullName = "Phạm Ngọc Lan",
                PhoneNumber = "0971648253",
                Relationship = "Vợ",
                CreatedAt = SeedAdmission,
            };
            await context.NextOfKins.AddRangeAsync(
                [kinA, kinB, removedKin, kinOfOther], TestContext.Current.CancellationToken);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return (main, other, kinA, kinB, kinOfOther, removedKin);
        }

        #region Happy path

        [Fact]
        public async Task UpdateNextOfKin_ValidUpdate_Updated_Success()
        {
            var (main, _, kinA, kinB, _, _) = await SeedAsync();
            var before = DateTimeOffset.UtcNow;
            var command = new UpdateNextOfKinCommand
            {
                Id = kinA.Id,
                // Lower-case and untrimmed input: the entity normalizes on assignment
                FullName = "  trần   đức mINH ",
                PhoneNumber = "0913458769",
                Relationship = "Con trai",
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            // The kin carries every new value - the name through the entity
            // normalizer - and gets its UpdatedAt re-stamped by the mapper
            await using var verify = new PatientDbContext(_options);
            var saved = await verify.NextOfKins.SingleAsync(
                x => x.Id == kinA.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Trần Đức Minh", saved.FullName);
            Assert.Equal(command.PhoneNumber, saved.PhoneNumber);
            Assert.Equal(command.Relationship, saved.Relationship);
            Assert.InRange(saved.UpdatedAt, before.AddMilliseconds(-1), DateTimeOffset.UtcNow.AddMilliseconds(1));

            // Identity, ownership and lifecycle fields are not part of the update
            Assert.Equal(kinA.Id, saved.Id);
            Assert.Equal(main.Id, saved.PatientId);
            Assert.Equal(kinA.CreatedAt, saved.CreatedAt);
            Assert.False(saved.IsDeleted);

            // The sibling kin of the same patient is not touched
            var sibling = await verify.NextOfKins.SingleAsync(
                x => x.Id == kinB.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Võ Quốc Huy", sibling.FullName);
            Assert.Equal("0376928415", sibling.PhoneNumber);
            Assert.Equal("Anh trai", sibling.Relationship);
        }

        [Fact]
        public async Task UpdateNextOfKin_KeepOwnPhoneNumber_Updated_Success()
        {
            /*
             * The uniqueness guard excludes the kin itself (x.Id != command.Id),
             * so an update that leaves the phone number as it is must succeed -
             * only the UpdatedAt stamp moves
             */
            var (_, _, kinA, _, _, _) = await SeedAsync();
            var before = DateTimeOffset.UtcNow;
            var command = new UpdateNextOfKinCommand
            {
                Id = kinA.Id,
                FullName = kinA.FullName,
                PhoneNumber = kinA.PhoneNumber, // the kin's own number
                Relationship = kinA.Relationship,
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var verify = new PatientDbContext(_options);
            var saved = await verify.NextOfKins.SingleAsync(
                x => x.Id == kinA.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Nguyễn Thị Mai Anh", saved.FullName);
            Assert.Equal("0987654321", saved.PhoneNumber);
            Assert.Equal("Mẹ", saved.Relationship);
            Assert.InRange(saved.UpdatedAt, before.AddMilliseconds(-1), DateTimeOffset.UtcNow.AddMilliseconds(1));
        }

        [Fact]
        public async Task UpdateNextOfKin_PhoneUsedByKinOfAnotherPatient_Updated_Success()
        {
            /*
             * Business rule (handler scope): the uniqueness check only compares
             * within the same patient (x.PatientId == nextOfKin.PatientId) - the
             * same person can be a registered next of kin of multiple patients
             */
            var (_, _, kinA, _, kinOfOther, _) = await SeedAsync();
            var command = new UpdateNextOfKinCommand
            {
                Id = kinA.Id,
                FullName = kinOfOther.FullName,
                PhoneNumber = kinOfOther.PhoneNumber, // already held by another patient's kin
                Relationship = kinOfOther.Relationship,
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var verify = new PatientDbContext(_options);
            var saved = await verify.NextOfKins.SingleAsync(
                x => x.Id == kinA.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Phạm Ngọc Lan", saved.FullName);
            Assert.Equal("0971648253", saved.PhoneNumber);
            Assert.Equal("Vợ", saved.Relationship);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task UpdateNextOfKin_UnknownId_Fail()
        {
            await SeedAsync();
            var command = new UpdateNextOfKinCommand
            {
                Id = Guid.CreateVersion7(),
                FullName = "Lâm Quang Vinh",
                PhoneNumber = "0932581746",
                Relationship = "Cha",
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The visible kin set stays exactly as seeded
            await using var verify = new PatientDbContext(_options);
            Assert.Equal(3, await verify.NextOfKins.CountAsync(
                TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task UpdateNextOfKin_SoftDeletedKin_Fail()
        {
            // A soft-deleted kin is hidden by the query filter, so the lookup is
            // rejected exactly like an unknown ID - and the stored row stays intact
            var (_, _, _, _, _, removedKin) = await SeedAsync();
            var command = new UpdateNextOfKinCommand
            {
                Id = removedKin.Id,
                FullName = "Đỗ Quang Minh",
                PhoneNumber = "0971648253",
                Relationship = "Con trai",
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var verify = new PatientDbContext(_options);
            var hidden = await verify.NextOfKins.IgnoreQueryFilters().SingleAsync(
                x => x.Id == removedKin.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Đỗ Thanh Tùng", hidden.FullName);
            Assert.Equal("0904518276", hidden.PhoneNumber);
            Assert.Equal("Con trai", hidden.Relationship);
        }

        [Fact]
        public async Task UpdateNextOfKin_DuplicatePhoneNumberSamePatient_Fail()
        {
            /*
             * Business rule (handler comment): phone number uniqueness within the
             * same patient - the sibling already holds the number, so the update
             * is rejected and the entity must stay untouched
             */
            var (_, _, kinA, kinB, _, _) = await SeedAsync();
            var command = new UpdateNextOfKinCommand
            {
                Id = kinA.Id,
                FullName = "Võ Minh Đạt",
                PhoneNumber = kinB.PhoneNumber, // already held by the sibling of the same patient
                Relationship = "Em trai",
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Nothing was written: the mapper never ran, so the stored values are
            // still the seeded ones
            await using var verify = new PatientDbContext(_options);
            var saved = await verify.NextOfKins.SingleAsync(
                x => x.Id == kinA.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Nguyễn Thị Mai Anh", saved.FullName);
            Assert.Equal("0987654321", saved.PhoneNumber);
            Assert.Equal("Mẹ", saved.Relationship);
        }

        #endregion
    }
}
