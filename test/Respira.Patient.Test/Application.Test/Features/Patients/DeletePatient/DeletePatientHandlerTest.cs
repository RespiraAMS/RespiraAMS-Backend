using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Application.Features.Patients.DeletePatient;
using Respira.Patient.Domain.Enums;
using Respira.Patient.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Test.Features.Patients.DeletePatient
{
    public class DeletePatientHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        // Fixed timestamps so "must not change" assertions can be exact
        private static readonly DateTimeOffset SeedAdmission = new(2026, 1, 15, 8, 30, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset SeedUpdatedAt = new(2026, 1, 15, 8, 45, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset SeedDeletedAt = new(2026, 9, 30, 14, 5, 0, TimeSpan.Zero);

        private readonly DbContextOptions<PatientDbContext> _options;
        private readonly DeletePatientHandler _handler;
        private readonly IDbContext _context;

        public DeletePatientHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<PatientDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new PatientDbContext(_options);
            var logger = new Mock<ILogger<DeletePatientHandler>>().Object;

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
        public async Task DeletePatient_WithoutTreatment_SoftDeletesPatient_Success()
        {
            /* Business rule (handler doc): a patient that received no treatment can be
             * removed - the record is soft-deleted (flagged, timestamped), not purged,
             * so the clinical history stays intact
             */
            var seeded = await SeedPatientAsync();
            var command = new DeletePatientCommand { Id = seeded.Id };
            var before = DateTimeOffset.UtcNow;

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            // Verify through a fresh context so the change tracker cannot mask a failed commit
            await using var freshContext = new PatientDbContext(_options);

            // Soft delete: the row survives, flagged and stamped at deletion time
            var row = await freshContext.Patients.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == seeded.Id, TestContext.Current.CancellationToken);
            Assert.True(row.IsDeleted);
            Assert.NotNull(row.DeletedAt);
            Assert.InRange(
                row.DeletedAt.Value,
                before.AddMilliseconds(-1),
                DateTimeOffset.UtcNow.AddMilliseconds(1));

            // Soft delete only flags the record - every clinical and demographic
            // field of the patient is retained verbatim
            Assert.Equal(seeded.FirstName, row.FirstName);
            Assert.Equal(seeded.LastName, row.LastName);
            Assert.Equal(seeded.PhoneNumber, row.PhoneNumber);
            Assert.Equal(seeded.DateOfBirth, row.DateOfBirth);
            Assert.Equal(seeded.Gender, row.Gender);
            Assert.Equal(seeded.Height, row.Height);
            Assert.Equal(seeded.Weight, row.Weight);
            Assert.Equal(seeded.MedicalRecordCode, row.MedicalRecordCode);
            Assert.Equal(seeded.HealthInsuranceCard, row.HealthInsuranceCard);
            Assert.Equal(seeded.Address, row.Address);
            Assert.Equal(seeded.City, row.City);
            Assert.Equal(SeedAdmission, row.Admission);

            // ... but it disappears from the filtered query every other handler uses
            Assert.False(await freshContext.Patients
                .AnyAsync(x => x.Id == seeded.Id, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeletePatient_WithNextOfKins_CascadeSoftDeletesOwnNextOfKins_Success()
        {
            /*
             * Business rule (handler doc): "Delete patient (and cascade its next of
             * kin if any)" - every next of kin of the deleted patient is soft-deleted
             * in the same transaction, while next of kin of other patients stay
             * untouched ("its" scopes the cascade to the one patient)
             */
            var seeded = await SeedPatientAsync();
            var other = await SeedPatientAsync(medicalRecordCode: "BN-2026-006044");
            await SeedNextOfKinAsync(seeded.Id, "Nguyễn Thị Mai Anh", "0987654321", "Mẹ");
            await SeedNextOfKinAsync(seeded.Id, "Trần Đức Minh", "0913458769", "Con trai");
            await SeedNextOfKinAsync(other.Id, "Lê Thu Hà", "0907835412", "Vợ");

            var command = new DeletePatientCommand { Id = seeded.Id };
            var before = DateTimeOffset.UtcNow;

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            await using var freshContext = new PatientDbContext(_options);

            // The deleted patient (and their next of kin) leave the filtered view
            Assert.False(await freshContext.Patients
                .AnyAsync(x => x.Id == seeded.Id, TestContext.Current.CancellationToken));
            Assert.Equal(0, await freshContext.NextOfKins
                .CountAsync(x => x.PatientId == seeded.Id, TestContext.Current.CancellationToken));

            // Cascade: both next of kin of the patient are soft-deleted with a fresh
            // deletion timestamp, and their rows survive the flagging
            var ownNextOfKins = await freshContext.NextOfKins.IgnoreQueryFilters()
                .Where(x => x.PatientId == seeded.Id)
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal(2, ownNextOfKins.Count);
            foreach (var nextOfKin in ownNextOfKins)
            {
                Assert.True(nextOfKin.IsDeleted);
                Assert.NotNull(nextOfKin.DeletedAt);
                Assert.InRange(
                    nextOfKin.DeletedAt.Value,
                    before.AddMilliseconds(-1),
                    DateTimeOffset.UtcNow.AddMilliseconds(1));
            }

            // Scope: the other patient's next of kin is not part of this cascade
            var foreignNextOfKin = await freshContext.NextOfKins.IgnoreQueryFilters()
                .SingleAsync(x => x.PatientId == other.Id, TestContext.Current.CancellationToken);
            Assert.False(foreignNextOfKin.IsDeleted);
            Assert.Null(foreignNextOfKin.DeletedAt);
            Assert.True(await freshContext.Patients
                .AnyAsync(x => x.Id == other.Id, TestContext.Current.CancellationToken));
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task DeletePatient_UnknownId_Fail()
        {
            var command = new DeletePatientCommand { Id = Guid.CreateVersion7() };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Nothing may exist after deleting something that never did
            await using var freshContext = new PatientDbContext(_options);
            Assert.Equal(0, await freshContext.Patients.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeletePatient_SoftDeletedPatient_Fail()
        {
            // A soft-deleted patient is hidden by the query filter, so a second
            // delete attempt is rejected like an unknown ID and changes nothing
            var seeded = await SeedPatientAsync(softDeleted: true);
            var command = new DeletePatientCommand { Id = seeded.Id };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new PatientDbContext(_options);
            var row = await freshContext.Patients.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == seeded.Id, TestContext.Current.CancellationToken);
            Assert.True(row.IsDeleted);
            // The original deletion timestamp is not re-stamped by the rejected attempt
            Assert.Equal(SeedDeletedAt, row.DeletedAt);
            Assert.Equal(seeded.FirstName, row.FirstName);
        }

        [Fact]
        public async Task DeletePatient_WithTreatment_Fail()
        {
            /*
             * Business rule (handler doc): "Only delete patient if they have receive
             * no treatment (e.g. doctor input wrong and want to delete)" - once a
             * treatment exists, the record is permanent and nothing is written: the
             * patient stays flagged as live
             */
            var seeded = await SeedPatientAsync(withTreatment: true);
            var command = new DeletePatientCommand { Id = seeded.Id };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new PatientDbContext(_options);
            var row = await freshContext.Patients.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == seeded.Id, TestContext.Current.CancellationToken);
            Assert.False(row.IsDeleted);
            Assert.Null(row.DeletedAt);
            Assert.Equal(seeded.FirstName, row.FirstName);
            Assert.Equal(seeded.MedicalRecordCode, row.MedicalRecordCode);

            // The treatment that blocks the deletion is still there
            Assert.Equal(1, await freshContext.Treatments
                .CountAsync(x => x.PatientId == seeded.Id, TestContext.Current.CancellationToken));
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

        private async Task SeedNextOfKinAsync(
            Guid patientId,
            string fullName,
            string phoneNumber,
            string relationship)
        {
            // Committed through a separate context, like the patient seed
            await using var context = new PatientDbContext(_options);
            await context.NextOfKins.AddAsync(new Domain.Entities.NextOfKin
            {
                PatientId = patientId,
                FullName = fullName,
                PhoneNumber = phoneNumber,
                Relationship = relationship,
            }, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
    }
}
