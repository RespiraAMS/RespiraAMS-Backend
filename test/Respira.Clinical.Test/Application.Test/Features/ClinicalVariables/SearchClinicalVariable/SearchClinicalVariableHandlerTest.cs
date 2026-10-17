using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.ClinicalVariables.SearchClinicalVariable;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Infrastructure.Data;
using Respira.Clinical.Infrastructure.Mapper;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Application.Test.Features.ClinicalVariables.SearchClinicalVariable
{
    public class SearchClinicalVariableHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly SearchClinicalVariableHandler _handler;
        private readonly IDbContext _context;

        public SearchClinicalVariableHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>()
                .UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
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
            // Clear leftover data so the counts are deterministic across runs.
            // IgnoreQueryFilters because soft deleted rows are hidden by the query
            // filter but still occupy the table
            await _context.ClinicalVariables.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        private static NumericClinicalVariable CreateNumeric(
            string name,
            string code,
            string description,
            ClinicalVariableCategory category,
            string unit,
            decimal acceptedMax,
            bool isRequired,
            bool isDeleted = false)
        {
            return new NumericClinicalVariable
            {
                Code = code,
                Name = name,
                Description = description,
                CanonicalUnit = unit,
                IsRequired = isRequired,
                Category = category,
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
                AcceptedRange = new Range
                {
                    Min = 0,
                    IsMinExclusive = false,
                    Max = acceptedMax,
                    IsMaxExclusive = false,
                    Unit = unit,
                },
            };
        }

        /*
         * Seven live variables plus one soft deleted variable. The word "blood"
         * appears in the name of five live variables (the blood pressure family and
         * blood glucose) and only in the description of C reactive protein, which
         * gives deterministic rank ordering.
         *
         * The last live variable is Vietnamese so accent folding can be exercised.
         */
        private async Task<IReadOnlyList<ClinicalVariable>> SeedMainAsync()
        {
            var seeded = new List<ClinicalVariable>
            {
                CreateNumeric(
                    "Systolic blood pressure",
                    "8480-6",
                    "Systolic blood pressure measured at the arm",
                    ClinicalVariableCategory.Clinical,
                    "mmHg",
                    300,
                    isRequired: true),
                CreateNumeric(
                    "Diastolic blood pressure",
                    "8462-4",
                    "Diastolic blood pressure measured at the arm",
                    ClinicalVariableCategory.Clinical,
                    "mmHg",
                    200,
                    isRequired: true),
                CreateNumeric(
                    "Mean arterial blood pressure",
                    "8478-0",
                    "Mean arterial blood pressure computed from the pulse wave",
                    ClinicalVariableCategory.Clinical,
                    "mmHg",
                    300,
                    isRequired: true),
                CreateNumeric(
                    "Blood glucose",
                    "2339-0",
                    "Blood glucose mass per volume in blood serum or plasma",
                    ClinicalVariableCategory.Paraclinical,
                    "mg/dL",
                    1000,
                    isRequired: true),
                CreateNumeric(
                    "C reactive protein",
                    "1988-5",
                    "C reactive protein mass per volume in blood serum or plasma",
                    ClinicalVariableCategory.Paraclinical,
                    "mg/L",
                    decimal.MaxValue,
                    isRequired: false),
                new BooleanClinicalVariable
                {
                    Code = "46098-0",
                    Name = "Female sex",
                    Description = "Whether the patient is female",
                    IsRequired = true,
                    Category = ClinicalVariableCategory.PersonalInformation,
                },
                new CategoricalClinicalVariable(["DETECTED", "NOT_DETECTED"])
                {
                    Code = "94500-6",
                    Name = "SARS-CoV-2 RNA",
                    Description = "SARS-CoV-2 RNA detected in respiratory specimen",
                    IsRequired = false,
                    Category = ClinicalVariableCategory.Paraclinical,
                },
                CreateNumeric(
                    "Cân nặng",
                    "29463-7",
                    "Cân nặng của bệnh nhân đo bằng kilogram",
                    ClinicalVariableCategory.Clinical,
                    "kg",
                    500,
                    isRequired: false),
            };

            await _context.ClinicalVariables.AddRangeAsync(seeded, TestContext.Current.CancellationToken);

            // Soft deleted: hidden by the query filter but still present in the table
            await _context.ClinicalVariables.AddAsync(
                CreateNumeric(
                    "Body temperature",
                    "8310-5",
                    "Body temperature measured axillary in degree Celsius",
                    ClinicalVariableCategory.Clinical,
                    "°C",
                    45,
                    isRequired: true,
                    isDeleted: true),
                TestContext.Current.CancellationToken);

            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return seeded;
        }

        #region Happy path

        [Fact]
        public async Task SearchClinicalVariable_PrefixMatchOnName_ReturnsFullProjection_Success()
        {
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "systolic",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Systolic blood pressure", item.Name);
            Assert.Equal("8480-6", item.Code);
            Assert.True(item.IsRequired);
            Assert.Equal("mmHg", item.CanonicalUnit);
            Assert.Equal(ClinicalValueType.Numeric, item.ValueType);
            Assert.Equal(ClinicalVariableCategory.Clinical, item.Category);
            Assert.True(item.Rank > 0);

            Assert.Equal(1, result.Data.Metadata.TotalItemCount);
            Assert.Equal(1, result.Data.Metadata.CurrentPage);
        }

        [Theory]
        [InlineData("systolic")] // full word
        [InlineData("Systolic")] // the respira_vi configuration lowercases both sides
        [InlineData("syst")]     // shorter prefix still matches
        public async Task SearchClinicalVariable_PrefixTerm_MatchesSameVariable_Success(string searchTerm)
        {
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Systolic blood pressure", item.Name);
        }

        [Fact]
        public async Task SearchClinicalVariable_BooleanVariable_ProjectsBooleanShape_Success()
        {
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "female",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Female sex", item.Name);
            Assert.Equal(ClinicalValueType.Boolean, item.ValueType);
            Assert.Equal(ClinicalVariableCategory.PersonalInformation, item.Category);
            Assert.True(item.IsRequired);
            // A boolean variable has no canonical unit
            Assert.Null(item.CanonicalUnit);
        }

        [Fact]
        public async Task SearchClinicalVariable_CategoricalVariable_ProjectsCategoricalShape_Success()
        {
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "sars",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("SARS-CoV-2 RNA", item.Name);
            Assert.Equal(ClinicalValueType.Categorical, item.ValueType);
            Assert.Equal(ClinicalVariableCategory.Paraclinical, item.Category);
            Assert.False(item.IsRequired);
            // A categorical variable has no canonical unit
            Assert.Null(item.CanonicalUnit);
        }

        [Fact]
        public async Task SearchClinicalVariable_MatchOnlyInDescription_ReturnsVariable_Success()
        {
            await SeedMainAsync();

            // "respiratory specimen" only appears together in the SARS-CoV-2 description,
            // so the multi word AND query narrows the result to a single variable
            var result = await _handler.HandleAsync(new SearchClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "respiratory specimen",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("SARS-CoV-2 RNA", item.Name);
            Assert.True(item.Rank > 0);
        }

        [Fact]
        public async Task SearchClinicalVariable_SingleWordInSeveralDescriptions_ReturnsAllMatches_Success()
        {
            await SeedMainAsync();

            // "arm" is a prefix of the lexeme "arm" and only appears in the description
            // of both blood pressure variables
            var result = await _handler.HandleAsync(new SearchClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "arm",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            var items = result.Data.Items.ToList();
            Assert.Equal(2, items.Count);
            Assert.Contains(items, x => x.Name == "Systolic blood pressure");
            Assert.Contains(items, x => x.Name == "Diastolic blood pressure");
            Assert.All(items, x => Assert.DoesNotContain("arm", x.Name, StringComparison.OrdinalIgnoreCase));
            Assert.All(items, x => Assert.True(x.Rank > 0));
        }

        [Theory]
        [InlineData("can")]  // unaccented query term
        [InlineData("cân")]  // accented query term
        public async Task SearchClinicalVariable_AccentFoldedTerm_MatchesVietnameseData_Success(string searchTerm)
        {
            await SeedMainAsync();

            // Both the stored vector and the query run through the respira_vi
            // configuration, which folds accents with unaccent
            var result = await _handler.HandleAsync(new SearchClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Cân nặng", item.Name);
            Assert.Equal("kg", item.CanonicalUnit);
            Assert.Equal(ClinicalValueType.Numeric, item.ValueType);
        }

        [Fact]
        public async Task SearchClinicalVariable_NameMatchOutranksDescriptionMatch_Success()
        {
            await SeedMainAsync();

            // "blood" appears in the name of five variables (weight A) and only in the
            // description of C reactive protein (weight C), and Glucose also has it in
            // its description so it collects A and C
            var result = await _handler.HandleAsync(new SearchClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "blood",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);

            var ordered = result.Data.Items.ToList();
            // Glucose matches its name and its description, so it outranks the
            // name-only matches, which in turn outrank the description-only match
            Assert.Equal("Blood glucose", ordered[0].Name);
            Assert.Equal("C reactive protein", ordered[^1].Name);
            Assert.True(ordered[0].Rank > ordered[1].Rank);
            Assert.True(ordered[^2].Rank > ordered[^1].Rank);
            Assert.All(ordered.Take(4), x => Assert.Contains("blood", x.Name, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain("blood", ordered[^1].Name, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SearchClinicalVariable_SameTermInDifferentFields_RanksNameAboveCodeAboveDescription_Success()
        {
            // A single term placed once in each searched field: name (weight A),
            // code (weight B) and description (weight C)
            await _context.ClinicalVariables.AddRangeAsync(
            [
                CreateNumeric(
                    "Glucose fasting",
                    "GLU-01",
                    "Measured after an overnight fast",
                    ClinicalVariableCategory.Paraclinical,
                    "mg/dL",
                    1000,
                    isRequired: true),
                CreateNumeric(
                    "Fasting plasma measurement",
                    "GLUCOSE",
                    "Measured after an overnight fast",
                    ClinicalVariableCategory.Paraclinical,
                    "mg/dL",
                    1000,
                    isRequired: false),
                CreateNumeric(
                    "Serum fasting value",
                    "GLU-03",
                    "Derived from the glucose value",
                    ClinicalVariableCategory.Paraclinical,
                    "mg/dL",
                    1000,
                    isRequired: false),
            ], TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(new SearchClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "glucose",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var ordered = result.Data.Items.ToList();
            Assert.Equal(
            [
                "Glucose fasting",             // name match
                "Fasting plasma measurement",  // code match
                "Serum fasting value",         // description match
            ], [.. ordered.Select(x => x.Name)]);
            Assert.True(ordered[0].Rank > ordered[1].Rank);
            Assert.True(ordered[1].Rank > ordered[2].Rank);
        }

        /*=== pagination ===*/

        [Fact]
        public async Task SearchClinicalVariable_FirstPage_SplitsMatchesWithMetadata_Success()
        {
            // 5 matches with page size 2 -> pages of [2, 2, 1]
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 2 },
                SearchTerm = "blood",
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
        public async Task SearchClinicalVariable_LastPage_HasNoNext_Success()
        {
            // Upper boundary page: the single leftover match, no next page
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 3, Size = 2 },
                SearchTerm = "blood",
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
        public async Task SearchClinicalVariable_MinimumPageSize_ReturnsOneItemPerPage_Success()
        {
            // Lower boundary of a usable page size
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 1 },
                SearchTerm = "blood",
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
        public async Task SearchClinicalVariable_MaximumPageSize_ReturnsAllMatchesInOnePage_Success()
        {
            // Upper boundary of a usable page size
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 100 },
                SearchTerm = "blood",
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
        public async Task SearchClinicalVariable_NoUsableLexeme_ReturnsEmptyPage_Success(string searchTerm)
        {
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchClinicalVariableQuery
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
        public async Task SearchClinicalVariable_TermMatchesNothing_ReturnsEmptyPage_Success()
        {
            await SeedMainAsync();

            // "troponin" is a real clinical variable but is not seeded here
            var result = await _handler.HandleAsync(new SearchClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "troponin",
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
        public async Task SearchClinicalVariable_TermIsInsideNameButNotPrefix_ReturnsEmptyPage_Success()
        {
            // "cose" appears inside "glucose" but matching is by prefix, so a term that
            // is only a suffix or an infix must not match
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "cose",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        #endregion

        #region Soft-deleted clinical variable

        [Fact]
        public async Task SearchClinicalVariable_SoftDeletedVariable_IsExcluded_Success()
        {
            await SeedMainAsync();

            // The soft deleted "Body temperature" is the only variable that matches
            // "temperature", so a non empty result would mean the query filter leaked
            var total = await _context.ClinicalVariables.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken);
            Assert.Equal(9, total);

            var result = await _handler.HandleAsync(new SearchClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "temperature",
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
         * These terms are also rejected by SearchClinicalVariableValidator, so in the
         * normal pipeline this is defence in depth for the shared utility.
         */
        [Theory]
        [InlineData("&&")]
        [InlineData("||")]
        [InlineData("!!")]
        [InlineData("((")]
        public async Task SearchClinicalVariable_SymbolOnlyTerm_ReturnsEmptyPage_Success(string searchTerm)
        {
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchClinicalVariableQuery
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
         * Fail path: SearchClinicalVariableHandler has no failure branch. It only ever
         * returns a success result (an empty page when nothing matches or the term has
         * no usable lexeme). Input validation happens earlier in
         * SearchClinicalVariableValidator, and the full text query cannot fail for
         * sanitised input, so there is no reachable failure result to assert here
         */
    }
}
