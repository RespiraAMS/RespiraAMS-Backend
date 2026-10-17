using Microsoft.EntityFrameworkCore;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Application.Contracts.Mappers;
using Respira.Patient.Application.Features.Patients.SearchPatient;
using Respira.Patient.Domain.Enums;
using Respira.Patient.Infrastructure.Data;
using Respira.Patient.Infrastructure.Mappers;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Test.Features.Patients.SearchPatient
{
    public class SearchPatientHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<PatientDbContext> _options;
        private readonly SearchPatientHandler _handler;
        private readonly IDbContext _context;

        public SearchPatientHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<PatientDbContext>()
                .UseNpgsql(fixture.ConnectionString).Options;
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
            // Treatments and next of kin reference patients through FKs, so delete them
            // first. IgnoreQueryFilters is needed because soft-deleted rows are hidden
            // by the query filter but still occupy the table
            await _context.Treatments.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.NextOfKins.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Patients.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        private static Domain.Entities.Patient CreatePatient(
            string firstName,
            string lastName,
            string medicalRecordCode,
            DateOnly dateOfBirth,
            Gender gender,
            TreatmentOutcome treatmentOutcome,
            string phoneNumber = "0912345678",
            string healthInsuranceCard = "8816372045",
            string address = "1 Lê Lợi, phường Bến Nghé",
            string city = "TP. Hồ Chí Minh",
            decimal height = 165.0m,
            decimal weight = 60.0m,
            bool isDeleted = false)
        {
            return new Domain.Entities.Patient
            {
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                DateOfBirth = dateOfBirth,
                Gender = gender,
                Height = height,
                Weight = weight,
                MedicalRecordCode = medicalRecordCode,
                HealthInsuranceCard = healthInsuranceCard,
                Address = address,
                City = city,
                Admission = DateTimeOffset.UtcNow.AddHours(-6),
                TreatmentOutcome = treatmentOutcome,
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
        }

        /*
         * Five live patients plus one soft-deleted patient. The search matches the
         * first name (weight A), the last name (weight B) and the medical record code
         * (weight C), so the names are chosen to make the ranking observable:
         * "minh" is the first name of one patient (A) and the last name of another (B).
         *
         * The last live patient uses accented Vietnamese names so accent folding can be
         * exercised.
         */
        private async Task<IReadOnlyList<Domain.Entities.Patient>> SeedMainAsync()
        {
            var thisYear = DateTimeOffset.UtcNow.Year;

            var seeded = new List<Domain.Entities.Patient>
            {
                CreatePatient(
                    "Minh Khuê",
                    "Đặng Thị",
                    "BN-2026-004817",
                    new DateOnly(thisYear - 34, 6, 18),
                    Gender.Female,
                    TreatmentOutcome.InTreatment,
                    phoneNumber: "0984372615",
                    height: 160.3m,
                    weight: 52.6m),
                CreatePatient(
                    "Tùng",
                    "Nguyễn Văn",
                    "BN-2026-005291",
                    new DateOnly(thisYear - 16, 3, 9),
                    Gender.Male,
                    TreatmentOutcome.InTreatment,
                    phoneNumber: "0912348765",
                    height: 175.4m,
                    weight: 64.2m),
                CreatePatient(
                    "Quân",
                    "Trần Hoàng Minh",
                    "BN-2026-006044",
                    new DateOnly(thisYear - 78, 5, 14),
                    Gender.Male,
                    TreatmentOutcome.Recovered,
                    phoneNumber: "0976251843",
                    height: 168.9m,
                    weight: 61.5m),
                CreatePatient(
                    "Hương",
                    "Phạm Thị",
                    "BN-2026-001203",
                    new DateOnly(thisYear - 45, 9, 27),
                    Gender.Female,
                    TreatmentOutcome.InTreatment,
                    phoneNumber: "0907835412",
                    height: 154.7m,
                    weight: 57.8m),
                CreatePatient(
                    "An",
                    "Lê Văn",
                    "BN-2026-008872",
                    new DateOnly(thisYear - 29, 12, 5),
                    Gender.Male,
                    TreatmentOutcome.Death,
                    phoneNumber: "0938614275",
                    height: 172.1m,
                    weight: 68.4m),
            };

            await _context.Patients.AddRangeAsync(seeded, TestContext.Current.CancellationToken);

            // Soft deleted: hidden by the query filter but still present in the table
            await _context.Patients.AddAsync(
                CreatePatient(
                    "Hồng",
                    "Vũ Thị",
                    "BN-2026-007315",
                    new DateOnly(thisYear - 50, 11, 2),
                    Gender.Female,
                    TreatmentOutcome.InTreatment,
                    phoneNumber: "0907835413",
                    height: 156.2m,
                    weight: 59.1m,
                    isDeleted: true),
                TestContext.Current.CancellationToken);

            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return seeded;
        }

        #region Happy path

        [Fact]
        public async Task SearchPatient_PrefixMatchOnFirstName_ReturnsFullProjection_Success()
        {
            var seeded = await SeedMainAsync();
            var tung = seeded.Single(x => x.FirstName == "Tùng");

            var result = await _handler.HandleAsync(new SearchPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "tùng",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal(tung.Id, item.Id);
            Assert.Equal("Nguyễn Văn Tùng", item.FullName);
            Assert.Equal(tung.DateOfBirth, item.DateOfBirth);
            // Age is calculated from the birth year only (entity rule)
            Assert.Equal(DateTimeOffset.Now.Year - tung.DateOfBirth.Year, item.Age);
            Assert.Equal(16, item.Age);
            Assert.Equal(Gender.Male, item.Gender);
            Assert.Equal("BN-2026-005291", item.MedicalRecordCode);
            Assert.Equal(TreatmentOutcome.InTreatment, item.TreatmentOutcome);
            Assert.True(item.Rank > 0);

            Assert.Equal(1, result.Data.Metadata.TotalItemCount);
            Assert.Equal(1, result.Data.Metadata.CurrentPage);
        }

        [Theory]
        [InlineData("tùng")]  // accented query term
        [InlineData("tung")]  // unaccented query term
        [InlineData("Tùng")]  // the respira_vi configuration lowercases both sides
        [InlineData("tù")]    // shorter prefix still matches
        public async Task SearchPatient_PrefixTerm_MatchesSamePatient_Success(string searchTerm)
        {
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Nguyễn Văn Tùng", item.FullName);
        }

        [Theory]
        [InlineData("dang", "Đặng Thị Minh Khuê")]   // unaccented query term, matches the last name "Đặng Thị"
        [InlineData("đặng", "Đặng Thị Minh Khuê")]   // accented query term
        [InlineData("huong", "Phạm Thị Hương")]      // unaccented query term, matches the first name "Hương"
        [InlineData("hương", "Phạm Thị Hương")]      // accented query term
        public async Task SearchPatient_AccentFoldedTerm_MatchesVietnameseData_Success(
            string searchTerm, string expectedFullName)
        {
            await SeedMainAsync();

            // Both the stored vector and the query run through the respira_vi
            // configuration, which folds accents with unaccent
            var result = await _handler.HandleAsync(new SearchPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal(expectedFullName, item.FullName);
        }

        [Fact]
        public async Task SearchPatient_MatchOnMedicalRecordCode_ReturnsPatient_Success()
        {
            await SeedMainAsync();

            // A code is one of the three searched fields (weight C)
            var result = await _handler.HandleAsync(new SearchPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "bn-2026-006044",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Trần Hoàng Minh Quân", item.FullName);
            Assert.Equal("BN-2026-006044", item.MedicalRecordCode);
            Assert.Equal(TreatmentOutcome.Recovered, item.TreatmentOutcome);
            Assert.True(item.Rank > 0);
        }

        [Fact]
        public async Task SearchPatient_MultiWordTerm_RequiresEveryWordInLastName_Success()
        {
            await SeedMainAsync();

            // "hoàng minh" only appears together in the last name (weight B) of one
            // patient, so the multi word AND query narrows the result to a single row
            var result = await _handler.HandleAsync(new SearchPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "hoàng minh",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Trần Hoàng Minh Quân", item.FullName);
            Assert.True(item.Rank > 0);
        }

        [Fact]
        public async Task SearchPatient_FirstNameMatchOutranksLastNameMatch_Success()
        {
            await SeedMainAsync();

            // "minh" is the first name of Đặng Thị Minh Khuê (weight A) and the last
            // name of Trần Hoàng Minh Quân (weight B), so the first name match wins
            var result = await _handler.HandleAsync(new SearchPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "minh",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var ordered = result.Data.Items.ToList();
            Assert.Equal(
            [
                "Đặng Thị Minh Khuê", // first name match (weight A)
                "Trần Hoàng Minh Quân", // last name match (weight B)
            ], [.. ordered.Select(x => x.FullName)]);
            Assert.True(ordered[0].Rank > ordered[1].Rank);
        }

        [Fact]
        public async Task SearchPatient_SameTermInDifferentFields_RanksFirstNameAboveLastNameAboveCode_Success()
        {
            // A single term placed once in each searched field: first name (weight A),
            // last name (weight B) and medical record code (weight C). The code uses a
            // custom tracking prefix, which is allowed because the code is doctor
            // supplied free text
            var thisYear = DateTimeOffset.UtcNow.Year;
            await _context.Patients.AddRangeAsync(
            [
                CreatePatient("Dương", "Nguyễn Văn", "BN-2026-000001",
                    new DateOnly(thisYear - 40, 1, 5), Gender.Male, TreatmentOutcome.InTreatment),
                CreatePatient("Thảo", "Dương", "BN-2026-000002",
                    new DateOnly(thisYear - 33, 4, 12), Gender.Female, TreatmentOutcome.InTreatment),
                CreatePatient("Hạnh", "Lê Thị", "BN-DUONG-2026-1",
                    new DateOnly(thisYear - 27, 8, 20), Gender.Female, TreatmentOutcome.InTreatment),
            ], TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(new SearchPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "duong",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var ordered = result.Data.Items.ToList();
            Assert.Equal(
            [
                "Nguyễn Văn Dương",    // first name match (weight A)
                "Dương Thảo",          // last name match (weight B)
                "Lê Thị Hạnh",         // code match (weight C)
            ], [.. ordered.Select(x => x.FullName)]);
            Assert.True(ordered[0].Rank > ordered[1].Rank);
            Assert.True(ordered[1].Rank > ordered[2].Rank);
        }

        /*=== pagination ===*/

        [Fact]
        public async Task SearchPatient_FirstPage_SplitsMatchesWithMetadata_Success()
        {
            // The "bn" lexeme is a part of every medical record code, so it matches all
            // 5 live patients; page size 2 -> pages of [2, 2, 1]
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 2 },
                SearchTerm = "bn",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Equal(2, result.Data.Items.Count());
            Assert.Equal(1, result.Data.Metadata.CurrentPage);
            Assert.Equal(2, result.Data.Metadata.PageSize);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.Equal(3, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasPreviousPage);
            Assert.True(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task SearchPatient_LastPage_HasNoNext_Success()
        {
            // Upper boundary page: the single leftover match, no next page
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchPatientQuery
            {
                Param = new PaginationParam { Page = 3, Size = 2 },
                SearchTerm = "bn",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.True(item.Rank > 0);
            Assert.Equal(3, result.Data.Metadata.CurrentPage);
            Assert.True(result.Data.Metadata.HasPreviousPage);
            Assert.False(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task SearchPatient_MinimumPageSize_ReturnsOneItemPerPage_Success()
        {
            // Lower boundary of a usable page size
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 1 },
                SearchTerm = "bn",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.True(item.Rank > 0);
            Assert.Equal(1, result.Data.Metadata.PageSize);
            Assert.Equal(5, result.Data.Metadata.PageCount);
            Assert.True(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task SearchPatient_MaximumPageSize_ReturnsAllMatchesInOnePage_Success()
        {
            // Upper boundary of a usable page size
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 100 },
                SearchTerm = "bn",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            Assert.Equal(5, result.Data.Items.Count());
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.Equal(1, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasNextPage);
        }

        #endregion

        #region Empty result

        [Theory]
        [InlineData("a b")]      // every word is a single character
        [InlineData("x y z")]    // same, with more unusable words
        public async Task SearchPatient_NoUsableLexeme_ReturnsEmptyPage_Success(string searchTerm)
        {
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchPatientQuery
            {
                // The short-circuit always reports an empty first page of size 10
                Param = new PaginationParam { Page = 3, Size = 5 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
            Assert.Equal(0, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasNextPage);
            Assert.False(result.Data.Metadata.HasPreviousPage);
            Assert.Equal(1, result.Data.Metadata.CurrentPage);
            Assert.Equal(10, result.Data.Metadata.PageSize);
        }

        [Fact]
        public async Task SearchPatient_TermMatchesNothing_ReturnsEmptyPage_Success()
        {
            await SeedMainAsync();

            // "ngô" is a common Vietnamese surname but no seeded patient carries it
            var result = await _handler.HandleAsync(new SearchPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "ngô",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
            Assert.Equal(0, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task SearchPatient_TermIsInsideNameButNotPrefix_ReturnsEmptyPage_Success()
        {
            // "uyên" appears inside "Nguyễn" but matching is by prefix, so a term that
            // is only a suffix or an infix must not match
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "uyên",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        #endregion

        #region Soft-deleted patient

        [Fact]
        public async Task SearchPatient_SoftDeletedPatient_IsExcluded_Success()
        {
            await SeedMainAsync();

            // The soft deleted "Vũ Thị Hồng" is the only patient that matches "hồng",
            // so a non empty result would mean the query filter leaked
            var total = await _context.Patients.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken);
            Assert.Equal(6, total);

            var result = await _handler.HandleAsync(new SearchPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "hồng",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        #endregion

        #region Symbol-only search term

        /*
         * FullTextSearchUtil.BuildPrefixQuery sanitises every character that is not a
         * letter, a digit or a hyphen, so a symbol-only term produces an empty tsquery
         * and the handler short-circuits to an empty page instead of sending malformed
         * tsquery syntax (& | ! ( ) : *) to PostgreSQL.
         *
         * These terms are also rejected by SearchPatientValidator, so in the normal
         * pipeline this is defence in depth for the shared utility.
         */
        [Theory]
        [InlineData("&&")]
        [InlineData("||")]
        [InlineData("!!")]
        [InlineData("((")]
        public async Task SearchPatient_SymbolOnlyTerm_ReturnsEmptyPage_Success(string searchTerm)
        {
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchPatientQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        #endregion

        /*
         * Fail path: SearchPatientHandler has no failure branch. It only ever returns a
         * success result (an empty page when nothing matches or the term has no usable
         * lexeme). Input validation happens earlier in SearchPatientValidator, and the
         * full text query cannot fail for sanitised input, so there is no reachable
         * failure result to assert here
         */
    }
}
