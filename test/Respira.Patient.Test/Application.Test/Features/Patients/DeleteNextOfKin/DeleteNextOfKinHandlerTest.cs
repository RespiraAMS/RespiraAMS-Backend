using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Application.Features.Patients.DeleteNextOfKin;
using Respira.Patient.Domain.Enums;
using Respira.Patient.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Test.Features.Patients.DeleteNextOfKin
{
    public class DeleteNextOfKinHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        // Fixed timestamps so "must not change" / exact equality assertions can be
        // exact (whole-second values survive the round-trip through timestamptz)
        private static readonly DateTimeOffset SeedAdmission = new(2026, 1, 15, 8, 30, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset SeedKinCreatedAt = new(2026, 1, 15, 8, 45, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset SeedDeletedAt = new(2026, 9, 30, 14, 5, 0, TimeSpan.Zero);

        private readonly DbContextOptions<PatientDbContext> _options;
        private readonly DeleteNextOfKinHandler _handler;
        private readonly IDbContext _context;

        public DeleteNextOfKinHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<PatientDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new PatientDbContext(_options);
            var logger = new Mock<ILogger<DeleteNextOfKinHandler>>().Object;

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
        public async Task DeleteNextOfKin_ValidKin_SoftDeletesKin_Success()
        {
            /*
             * Business rule (handler): deleting a next of kin flags the record - it
             * is a soft delete (IsDeleted + DeletedAt), not a purge - so the audit
             * trail of who was reachable stays intact while the kin leaves the
             * filtered view every other handler uses
             */
            var patient = await SeedPatientAsync();
            var kin = await SeedNextOfKinAsync(
                patient.Id, "Nguyễn Thị Mai Anh", "0987654321", "Mẹ");
            var before = DateTimeOffset.UtcNow;

            var result = await _handler.HandleAsync(
                new DeleteNextOfKinCommand { Id = kin.Id }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            // Verify through a fresh context so the change tracker cannot mask a failed commit
            await using var freshContext = new PatientDbContext(_options);

            // Soft delete: the row survives, flagged and stamped at deletion time
            var row = await freshContext.NextOfKins.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == kin.Id, TestContext.Current.CancellationToken);
            Assert.True(row.IsDeleted);
            Assert.NotNull(row.DeletedAt);
            Assert.InRange(
                row.DeletedAt.Value,
                before.AddMilliseconds(-1),
                DateTimeOffset.UtcNow.AddMilliseconds(1));

            // Soft delete only flags the record - identity, ownership and contact
            // data are retained verbatim
            Assert.Equal(kin.Id, row.Id);
            Assert.Equal(patient.Id, row.PatientId);
            Assert.Equal(kin.FullName, row.FullName);
            Assert.Equal(kin.PhoneNumber, row.PhoneNumber);
            Assert.Equal(kin.Relationship, row.Relationship);
            Assert.Equal(SeedKinCreatedAt, row.CreatedAt);

            // ... but it disappears from the filtered query
            Assert.False(await freshContext.NextOfKins
                .AnyAsync(x => x.Id == kin.Id, TestContext.Current.CancellationToken));

            // The patient is not part of this operation: still live and visible
            var patientRow = await freshContext.Patients
                .SingleAsync(x => x.Id == patient.Id, TestContext.Current.CancellationToken);
            Assert.False(patientRow.IsDeleted);
            Assert.Null(patientRow.DeletedAt);
        }

        [Fact]
        public async Task DeleteNextOfKin_LeavesSiblingsAndOtherPatientsUntouched_Success()
        {
            /*
             * Scope of the operation: only the addressed kin is flagged - the
             * sibling kin of the same patient, the kin of a foreign patient and
             * both patients themselves stay live (unlike the patient deletion,
             * which cascades onto its kin, no cascade runs in the other direction)
             */
            var patient = await SeedPatientAsync();
            var target = await SeedNextOfKinAsync(
                patient.Id, "Nguyễn Thị Mai Anh", "0987654321", "Mẹ");
            var sibling = await SeedNextOfKinAsync(
                patient.Id, "Trần Đức Minh", "0913458769", "Con trai");
            var other = await SeedPatientAsync(medicalRecordCode: "BN-2026-006044");
            var foreignKin = await SeedNextOfKinAsync(
                other.Id, "Lê Thu Hà", "0907835412", "Vợ");

            var result = await _handler.HandleAsync(
                new DeleteNextOfKinCommand { Id = target.Id }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            await using var freshContext = new PatientDbContext(_options);

            // The sibling keeps its contact data and its live status
            var siblingRow = await freshContext.NextOfKins
                .SingleAsync(x => x.Id == sibling.Id, TestContext.Current.CancellationToken);
            Assert.False(siblingRow.IsDeleted);
            Assert.Null(siblingRow.DeletedAt);
            Assert.Equal("Trần Đức Minh", siblingRow.FullName);
            Assert.Equal("0913458769", siblingRow.PhoneNumber);
            Assert.Equal(1, await freshContext.NextOfKins
                .CountAsync(x => x.PatientId == patient.Id, TestContext.Current.CancellationToken));

            // The foreign kin and both patients are untouched
            var foreignRow = await freshContext.NextOfKins
                .SingleAsync(x => x.Id == foreignKin.Id, TestContext.Current.CancellationToken);
            Assert.False(foreignRow.IsDeleted);
            Assert.Null(foreignRow.DeletedAt);
            Assert.True(await freshContext.Patients
                .AnyAsync(x => x.Id == patient.Id, TestContext.Current.CancellationToken));
            Assert.True(await freshContext.Patients
                .AnyAsync(x => x.Id == other.Id, TestContext.Current.CancellationToken));
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task DeleteNextOfKin_UnknownId_Fail()
        {
            var command = new DeleteNextOfKinCommand { Id = Guid.CreateVersion7() };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Nothing may exist after deleting something that never did
            await using var freshContext = new PatientDbContext(_options);
            Assert.Equal(0, await freshContext.NextOfKins.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteNextOfKin_SoftDeletedKin_Fail()
        {
            // A soft-deleted kin is hidden by the query filter, so a second delete
            // attempt is rejected like an unknown ID and changes nothing
            var patient = await SeedPatientAsync();
            var kin = await SeedNextOfKinAsync(
                patient.Id, "Đỗ Thanh Tùng", "0904518276", "Con trai", softDeleted: true);
            var command = new DeleteNextOfKinCommand { Id = kin.Id };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new PatientDbContext(_options);
            var row = await freshContext.NextOfKins.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == kin.Id, TestContext.Current.CancellationToken);
            Assert.True(row.IsDeleted);
            // The original deletion timestamp is not re-stamped by the rejected attempt
            Assert.Equal(SeedDeletedAt, row.DeletedAt);
            Assert.Equal("Đỗ Thanh Tùng", row.FullName);
            Assert.Equal("0904518276", row.PhoneNumber);
        }

        #endregion

        private async Task<Domain.Entities.Patient> SeedPatientAsync(
            string medicalRecordCode = "BN-2026-004817")
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
            };
            await context.Patients.AddAsync(patient, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return patient;
        }

        private async Task<Domain.Entities.NextOfKin> SeedNextOfKinAsync(
            Guid patientId,
            string fullName,
            string phoneNumber,
            string relationship,
            bool softDeleted = false)
        {
            // Committed through a separate context, like the patient seed; the
            // whole-second CreatedAt makes the exact round-trip through the
            // database deterministic
            await using var context = new PatientDbContext(_options);
            var kin = new Domain.Entities.NextOfKin
            {
                PatientId = patientId,
                FullName = fullName,
                PhoneNumber = phoneNumber,
                Relationship = relationship,
                CreatedAt = SeedKinCreatedAt,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? SeedDeletedAt : null,
            };
            await context.NextOfKins.AddAsync(kin, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return kin;
        }
    }
}
