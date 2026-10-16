using Microsoft.EntityFrameworkCore;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Application.Features.Patients.GetPatientById;
using Respira.Patient.Domain.Enums;
using Respira.Patient.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Test.Features.Patients.GetPatientById
{
    public class GetPatientByIdHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        // Fixed timestamps so "exactly equals" assertions can be exact
        private static readonly DateTimeOffset SeedAdmission = new(2026, 1, 15, 8, 30, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset SeedDischarge = new(2026, 2, 3, 10, 15, 0, TimeSpan.Zero);

        private readonly DbContextOptions<PatientDbContext> _options;
        private readonly GetPatientByIdHandler _handler;
        private readonly IDbContext _context;

        public GetPatientByIdHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<PatientDbContext>().UseNpgsql(fixture.ConnectionString).Options;
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
            await _context.Treatments.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.NextOfKins.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Patients.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        #region Happy path

        [Fact]
        public async Task GetPatientById_DischargedPatientWithRelations_ReturnsFullGraph_Success()
        {
            /*
             * A complete record: the discharge fields, one treatment episode and two
             * next of kin - every field of the projection must carry the stored value,
             * and each NextOfKinItem must describe the next of kin themselves
             * (their own name, phone and relationship to the patient)
             */
            var (seeded, treatment, kinMother, kinChild) = await SeedFullGraphAsync();

            var result = await _handler.HandleAsync(
                new GetPatientByIdQuery { Id = seeded.Id }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var data = result.Data;
            Assert.Equal(seeded.Id, data.Id);
            // FullName is derived as "{LastName} {FirstName}" on the entity
            Assert.Equal("Trần Hoàng Minh Quân", data.FullName);
            Assert.Equal(seeded.PhoneNumber, data.PhoneNumber);
            Assert.Equal(seeded.DateOfBirth, data.DateOfBirth);
            Assert.Equal(Gender.Male, data.Gender);
            Assert.Equal(172.5m, data.Height);
            Assert.Equal(68.4m, data.Weight);
            Assert.Equal("BN-2026-004817", data.MedicalRecordCode);
            Assert.Equal("8816372045", data.HealthInsuranceCard);
            Assert.Equal("27 Nguyễn Huệ, phường Bến Nghé", data.Address);
            Assert.Equal("TP. Hồ Chí Minh", data.City);
            Assert.Equal(SeedAdmission, data.Admission);
            Assert.Equal(SeedDischarge, data.Discharge);
            Assert.Equal(TreatmentOutcome.Recovered, data.TreatmentOutcome);

            // The nested treatment projection carries the episode's identity
            var treatmentItem = Assert.Single(data.Treatments);
            Assert.Equal(treatment.Id, treatmentItem.Id);

            // The nested next-of-kin projection describes each kin, not the patient
            Assert.Equal(2, data.NextOfKins.Count());
            var mother = data.NextOfKins.Single(x => x.Id == kinMother.Id);
            Assert.Equal("Nguyễn Thị Mai Anh", mother.FullName);
            Assert.Equal("0987654321", mother.PhoneNumber);
            Assert.Equal("Mẹ", mother.Relationship);

            var child = data.NextOfKins.Single(x => x.Id == kinChild.Id);
            Assert.Equal("Trần Đức Minh", child.FullName);
            Assert.Equal("0913458769", child.PhoneNumber);
            Assert.Equal("Con trai", child.Relationship);
        }

        [Fact]
        public async Task GetPatientById_PatientWithoutRelations_ReturnsEmptyCollections_Success()
        {
            // Boundary: a live patient with no discharge, no treatment and no next of
            // kin - the collection projections come back empty and the discharge
            // fields carry their not-yet-set defaults
            var seeded = await SeedPlainPatientAsync();

            var result = await _handler.HandleAsync(
                new GetPatientByIdQuery { Id = seeded.Id }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var data = result.Data;
            Assert.Equal(seeded.Id, data.Id);
            Assert.Equal("Nguyễn Thị Mỹ Linh", data.FullName);
            Assert.Equal("0976251843", data.PhoneNumber);
            Assert.Equal(new DateOnly(1996, 7, 22), data.DateOfBirth);
            Assert.Equal(Gender.Female, data.Gender);
            Assert.Equal(156.8m, data.Height);
            Assert.Equal(48.9m, data.Weight);
            Assert.Equal("BN-2026-008127", data.MedicalRecordCode);
            Assert.Null(data.Discharge);
            Assert.Equal(TreatmentOutcome.InTreatment, data.TreatmentOutcome);
            Assert.Empty(data.NextOfKins);
            Assert.Empty(data.Treatments);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task GetPatientById_UnknownId_Fail()
        {
            var result = await _handler.HandleAsync(
                new GetPatientByIdQuery { Id = Guid.CreateVersion7() }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
        }

        [Fact]
        public async Task GetPatientById_SoftDeletedPatient_Fail()
        {
            // A soft-deleted patient is hidden by the query filter, so the lookup is
            // rejected exactly like an unknown ID
            var seeded = await SeedPlainPatientAsync(softDeleted: true);

            var result = await _handler.HandleAsync(
                new GetPatientByIdQuery { Id = seeded.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
        }

        #endregion

        private async Task<(
            Domain.Entities.Patient Patient,
            Domain.Entities.Treatment Treatment,
            Domain.Entities.NextOfKin KinMother,
            Domain.Entities.NextOfKin KinChild)> SeedFullGraphAsync()
        {
            // Committed through a separate context so the handler's change tracker
            // never sees the rows: the returned entities stay pristine snapshots of
            // the seeded values for later assertions
            await using var context = new PatientDbContext(_options);
            var patient = new Domain.Entities.Patient
            {
                FirstName = "Quân",
                LastName = "Trần Hoàng Minh",
                PhoneNumber = "0912348765",
                DateOfBirth = new DateOnly(1948, 5, 14),
                Gender = Gender.Male,
                Height = 172.5m,
                Weight = 68.4m,
                MedicalRecordCode = "BN-2026-004817",
                HealthInsuranceCard = "8816372045",
                Address = "27 Nguyễn Huệ, phường Bến Nghé",
                City = "TP. Hồ Chí Minh",
                Admission = SeedAdmission,
                Discharge = SeedDischarge,
                TreatmentOutcome = TreatmentOutcome.Recovered,
            };
            await context.Patients.AddAsync(patient, TestContext.Current.CancellationToken);

            var treatment = new Domain.Entities.Treatment
            {
                PatientId = patient.Id,
            };
            await context.Treatments.AddAsync(treatment, TestContext.Current.CancellationToken);

            var kinMother = new Domain.Entities.NextOfKin
            {
                PatientId = patient.Id,
                FullName = "Nguyễn Thị Mai Anh",
                PhoneNumber = "0987654321",
                Relationship = "Mẹ",
            };
            var kinChild = new Domain.Entities.NextOfKin
            {
                PatientId = patient.Id,
                FullName = "Trần Đức Minh",
                PhoneNumber = "0913458769",
                Relationship = "Con trai",
            };
            await context.NextOfKins.AddRangeAsync(
                [kinMother, kinChild], TestContext.Current.CancellationToken);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return (patient, treatment, kinMother, kinChild);
        }

        private async Task<Domain.Entities.Patient> SeedPlainPatientAsync(bool softDeleted = false)
        {
            await using var context = new PatientDbContext(_options);
            var patient = new Domain.Entities.Patient
            {
                FirstName = "Mỹ Linh",
                LastName = "Nguyễn Thị",
                PhoneNumber = "0976251843",
                DateOfBirth = new DateOnly(1996, 7, 22),
                Gender = Gender.Female,
                Height = 156.8m,
                Weight = 48.9m,
                MedicalRecordCode = "BN-2026-008127",
                HealthInsuranceCard = "7016382459",
                Address = "18 Lý Thái Tổ, quận Hoàn Kiếm",
                City = "Hà Nội",
                Admission = SeedAdmission,
                TreatmentOutcome = TreatmentOutcome.InTreatment,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? SeedDischarge : null,
            };
            await context.Patients.AddAsync(patient, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return patient;
        }
    }
}
