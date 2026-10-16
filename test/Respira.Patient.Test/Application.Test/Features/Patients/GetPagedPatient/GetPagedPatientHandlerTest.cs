using Microsoft.EntityFrameworkCore;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Application.Contracts.Mappers;
using Respira.Patient.Application.Features.Patients.GetPagedPatient;
using Respira.Patient.Domain.Enums;
using Respira.Patient.Infrastructure.Data;
using Respira.Patient.Infrastructure.Mappers;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Test.Features.Patients.GetPagedPatient
{
    public class GetPagedPatientHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<PatientDbContext> _options;
        private readonly GetPagedPatientHandler _handler;
        private readonly IDbContext _context;

        public GetPagedPatientHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<PatientDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new PatientDbContext(_options);
            IPaginationFactory factory = new PaginationFactory();

            // Initialize handler
            _handler = new(_context, factory);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Treatments reference patients through FKs, so delete them first.
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
         * Seeds four patients whose CreatedAt are spaced 1 minute apart so that the
         * CreatedAt-descending order of the handler is deterministic, plus one
         * soft-deleted patient that no listing may ever include.
         * Live, newest first: Minh Khuê (34, F) -> Tùng (16, M) -> Quân (78, M,
         * discharged with a treatment episode). The ages sit on the boundaries of
         * the MinAge/MaxAge filters below, and the year-only age rule makes every
         * date inside the birth year equally valid
         */
        private async Task<(Domain.Entities.Patient Kieu, Domain.Entities.Patient Tung, Domain.Entities.Patient Quan)> SeedAsync()
        {
            await using var context = new PatientDbContext(_options);
            var baseTime = DateTimeOffset.UtcNow;
            var thisYear = DateTimeOffset.UtcNow.Year;

            var kieu = new Domain.Entities.Patient
            {
                FirstName = "Minh Khuê",
                LastName = "Đặng Thị",
                PhoneNumber = "0984372615",
                DateOfBirth = new DateOnly(thisYear - 34, 6, 18),
                Gender = Gender.Female,
                Height = 160.3m,
                Weight = 52.6m,
                MedicalRecordCode = "BN-2026-004817",
                HealthInsuranceCard = "8816372045",
                Address = "18 Lý Thái Tổ, quận Hoàn Kiếm",
                City = "Hà Nội",
                Admission = baseTime.AddMinutes(-30),
                TreatmentOutcome = TreatmentOutcome.InTreatment,
                CreatedAt = baseTime,
            };
            var tung = new Domain.Entities.Patient
            {
                FirstName = "Tùng",
                LastName = "Nguyễn Văn",
                PhoneNumber = "0912348765",
                DateOfBirth = new DateOnly(thisYear - 16, 3, 9),
                Gender = Gender.Male,
                Height = 175.4m,
                Weight = 64.2m,
                MedicalRecordCode = "BN-2026-005291",
                HealthInsuranceCard = "7016382459",
                Address = "27 Nguyễn Huệ, phường Bến Nghé",
                City = "TP. Hồ Chí Minh",
                Admission = baseTime.AddMinutes(-25),
                TreatmentOutcome = TreatmentOutcome.InTreatment,
                CreatedAt = baseTime.AddMinutes(-1),
            };
            var quan = new Domain.Entities.Patient
            {
                FirstName = "Quân",
                LastName = "Trần Hoàng Minh",
                PhoneNumber = "0976251843",
                DateOfBirth = new DateOnly(thisYear - 78, 5, 14),
                Gender = Gender.Male,
                Height = 168.9m,
                Weight = 61.5m,
                MedicalRecordCode = "BN-2026-006044",
                HealthInsuranceCard = "8816372046",
                Address = "59 Phố Huế, quận Hoàn Kiếm",
                City = "Hà Nội",
                Admission = baseTime.AddMinutes(-20),
                Discharge = baseTime.AddMinutes(-10),
                TreatmentOutcome = TreatmentOutcome.Recovered,
                CreatedAt = baseTime.AddMinutes(-2),
            };
            var removed = new Domain.Entities.Patient
            {
                FirstName = "Hồng",
                LastName = "Vũ Thị",
                PhoneNumber = "0907835412",
                DateOfBirth = new DateOnly(thisYear - 50, 11, 2),
                Gender = Gender.Female,
                Height = 154.7m,
                Weight = 57.8m,
                MedicalRecordCode = "BN-2026-007315",
                HealthInsuranceCard = "7016382460",
                Address = "12 Trần Phú, quận Ninh Kiều",
                City = "Cần Thơ",
                Admission = baseTime.AddMinutes(-15),
                TreatmentOutcome = TreatmentOutcome.InTreatment,
                IsDeleted = true,
                DeletedAt = baseTime.AddMinutes(-5),
                CreatedAt = baseTime.AddMinutes(-3),
            };

            await context.Patients.AddRangeAsync(
                [kieu, tung, quan, removed], TestContext.Current.CancellationToken);
            await context.Treatments.AddAsync(new Domain.Entities.Treatment
            {
                PatientId = quan.Id,
            }, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return (kieu, tung, quan);
        }

        #region Happy path

        [Fact]
        public async Task GetPagedPatient_NoFilter_FirstPageNewestFirstWithMetadata_Success()
        {
            var (kieu, _, _) = await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 2 },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            // Newest first; the soft-deleted patient never counts towards the list
            Assert.Equal(
            [
                "Đặng Thị Minh Khuê",
                "Nguyễn Văn Tùng",
            ], [.. result.Data.Items.Select(x => x.FullName)]);

            var item = result.Data.Items.Single(x => x.FullName == "Đặng Thị Minh Khuê");
            Assert.Equal(kieu.Id, item.Id);
            Assert.Equal(new DateOnly(DateTimeOffset.UtcNow.Year - 34, 6, 18), item.DateOfBirth);
            // Age is calculated from the birth year only (entity rule)
            Assert.Equal(34, item.Age);
            Assert.Equal(Gender.Female, item.Gender);
            Assert.Equal("BN-2026-004817", item.MedicalRecordCode);
            Assert.Equal(TreatmentOutcome.InTreatment, item.TreatmentOutcome);

            Assert.Equal(1, result.Data.Metadata.CurrentPage);
            Assert.Equal(2, result.Data.Metadata.PageSize);
            Assert.Equal(3, result.Data.Metadata.TotalItemCount);
            Assert.Equal(2, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasPreviousPage);
            Assert.True(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task GetPagedPatient_LastPartialPage_HasNoNext_Success()
        {
            var (_, _, quan) = await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedPatientQuery
            {
                Param = new PaginationParam { Page = 2, Size = 2 },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            // Upper boundary page: only the oldest patient is left, already discharged
            var item = Assert.Single(result.Data.Items);
            Assert.Equal(quan.Id, item.Id);
            Assert.Equal("Trần Hoàng Minh Quân", item.FullName);
            Assert.Equal(78, item.Age);
            Assert.Equal(TreatmentOutcome.Recovered, item.TreatmentOutcome);
            Assert.True(result.Data.Metadata.HasPreviousPage);
            Assert.False(result.Data.Metadata.HasNextPage);
            Assert.Equal(2, result.Data.Metadata.CurrentPage);
            Assert.Equal(3, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedPatient_NullFilter_ReturnsEveryLivePatient_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = null,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(
            [
                "Đặng Thị Minh Khuê",
                "Nguyễn Văn Tùng",
                "Trần Hoàng Minh Quân",
            ], [.. result.Data.Items.Select(x => x.FullName)]);
            Assert.Equal(3, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedPatient_GenderFilter_ReturnsOnlyThatGender_Success()
        {
            var (kieu, _, _) = await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new PatientFilter { Gender = Gender.Female },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal(kieu.Id, item.Id);
            Assert.Equal(Gender.Female, item.Gender);
            Assert.Equal(1, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedPatient_MinAgeFilter_ExcludesBelowBoundary_Success()
        {
            /*
             * Boundary of "Age() >= MinAge": the patients are 16, 34 and 78 years
             * old, so MinAge 17 is the first value that drops the 16 year old, and
             * the rule stays inclusive for everyone above
             */
            var (kieu, _, quan) = await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new PatientFilter { MinAge = 17 },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(
            [
                kieu.FullName,
                quan.FullName,
            ], [.. result.Data.Items.Select(x => x.FullName)]);
            Assert.Equal(2, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedPatient_MaxAgeFilter_IncludesBoundaryAge_Success()
        {
            /*
             * Boundary of "Age() <= MaxAge": MaxAge 34 equals the age of one patient
             * exactly - the rule is inclusive - while the 78 year old is excluded
             */
            var (kieu, tung, _) = await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new PatientFilter { MaxAge = 34 },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(
            [
                kieu.FullName,
                tung.FullName,
            ], [.. result.Data.Items.Select(x => x.FullName)]);
            Assert.Equal(2, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedPatient_MinAndMaxAgeRange_AppliedTogether_Success()
        {
            // Both bounds together: 17..34 keeps only the 34 year old
            var (kieu, _, _) = await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new PatientFilter { MinAge = 17, MaxAge = 34 },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal(kieu.Id, item.Id);
            Assert.Equal(1, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedPatient_CombinedFilters_AppliedTogether_Success()
        {
            // Two independent criteria must both hold: gender AND min age 
            var (_, _, quan) = await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new PatientFilter
                {
                    Gender = Gender.Male,
                    MinAge = 50,
                },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal(quan.Id, item.Id);
            Assert.Equal(Gender.Male, item.Gender);
            Assert.True(item.Age >= 50);
        }

        [Fact]
        public async Task GetPagedPatient_FilterMatchesNothing_ReturnsEmpty_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new PatientFilter { MaxAge = 10 },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedPatient_PageBeyondRangeWithMinimumSize_ReturnsEmptyPage_Success()
        {
            /*
             * Boundary: Size 1 is the smallest allowed page size, and page 4 does not
             * exist for three patients - a stale page request must answer with an
             * empty page, not an error
             */
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedPatientQuery
            {
                Param = new PaginationParam { Page = 4, Size = 1 },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Empty(result.Data.Items);
            Assert.Equal(3, result.Data.Metadata.TotalItemCount);
            Assert.False(result.Data.Metadata.HasNextPage);
        }

        #endregion
    }
}
