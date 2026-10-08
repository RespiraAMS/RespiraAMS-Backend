using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.ClinicalVariables.GetPagedClinicalVariable;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Infrastructure.Data;
using Respira.Clinical.Infrastructure.Mapper;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Application.Test.Features.ClinicalVariables.GetPagedClinicalVariable
{
    public class GetPagedClinicalVariableHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly GetPagedClinicalVariableHandler _handler;
        private readonly IDbContext _context;

        public GetPagedClinicalVariableHandlerTest(PostgresFixture fixture)
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
            // Clear leftover data so the ordering / count assertions are deterministic
            // across runs. IgnoreQueryFilters because soft deleted rows are hidden by the
            // query filter but still occupy the table
            await _context.ClinicalVariables.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        /*
         * Seeds five live variables whose CreatedAt are spaced 1 minute apart so that the
         * CreatedAt-descending order of the handler is deterministic, plus one soft deleted
         * variable that must never show up.
         *
         * Newest first: Systolic blood pressure, SARS-CoV-2 RNA, Female sex,
         * C reactive protein, Blood glucose
         */
        private async Task SeedAsync()
        {
            var baseTime = DateTimeOffset.UtcNow;

            await _context.ClinicalVariables.AddRangeAsync(
            [
                // LOINC 8480-6
                CreateNumeric(
                    "Systolic blood pressure",
                    "8480-6",
                    "Systolic blood pressure measured at the arm",
                    ClinicalVariableCategory.Clinical,
                    "mmHg",
                    300,
                    isRequired: true,
                    createdAt: baseTime),
                // LOINC 1988-5
                CreateNumeric(
                    "C reactive protein",
                    "1988-5",
                    "C reactive protein mass per volume in serum or plasma",
                    ClinicalVariableCategory.Paraclinical,
                    "mg/L",
                    decimal.MaxValue,
                    isRequired: false,
                    createdAt: baseTime.AddMinutes(-2)),
                // LOINC 2339-0
                CreateNumeric(
                    "Blood glucose",
                    "2339-0",
                    "Blood glucose mass per volume in serum or plasma",
                    ClinicalVariableCategory.Paraclinical,
                    "mg/dL",
                    1000,
                    isRequired: true,
                    createdAt: baseTime.AddMinutes(-4)),
                // LOINC 46098-0
                new BooleanClinicalVariable
                {
                    Code = "46098-0",
                    Name = "Female sex",
                    Description = "Whether the patient is female",
                    IsRequired = true,
                    Category = ClinicalVariableCategory.PersonalInformation,
                    CreatedAt = baseTime.AddMinutes(-3),
                },
                // LOINC 94500-6
                new CategoricalClinicalVariable(["DETECTED", "NOT_DETECTED"])
                {
                    Code = "94500-6",
                    Name = "SARS-CoV-2 RNA",
                    Description = "SARS-CoV-2 RNA detected in respiratory specimen",
                    IsRequired = false,
                    Category = ClinicalVariableCategory.Paraclinical,
                    CreatedAt = baseTime.AddMinutes(-1),
                },
                // Soft deleted: hidden by the query filter but still in the table
                CreateNumeric(
                    "Body temperature",
                    "8310-5",
                    "Body temperature measured axillary in degree Celsius",
                    ClinicalVariableCategory.Clinical,
                    "°C",
                    45,
                    isRequired: true,
                    createdAt: baseTime.AddMinutes(-5),
                    isDeleted: true),
            ], TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        #region Happy path

        [Fact]
        public async Task GetPagedClinicalVariable_NoFilter_FirstPageNewestFirst_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 2 },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(
            [
                "Systolic blood pressure",
                "SARS-CoV-2 RNA",
            ], [.. result.Data.Items.Select(x => x.Name)]);

            Assert.Equal(1, result.Data.Metadata.CurrentPage);
            Assert.Equal(2, result.Data.Metadata.PageSize);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.Equal(3, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasPreviousPage);
            Assert.True(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task GetPagedClinicalVariable_LastPartialPage_HasNoNext_Success()
        {
            // Upper boundary page: only 1 leftover item and no next page
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 3, Size = 2 },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Blood glucose", item.Name);
            Assert.True(result.Data.Metadata.HasPreviousPage);
            Assert.False(result.Data.Metadata.HasNextPage);
            Assert.Equal(3, result.Data.Metadata.CurrentPage);
        }

        [Fact]
        public async Task GetPagedClinicalVariable_PageBeyondRange_ReturnsNoItems()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 4, Size = 2 },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.False(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task GetPagedClinicalVariable_ProjectsAllItemFields_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            var sbp = Assert.Single(result.Data.Items, x => x.Code == "8480-6");
            Assert.Equal("Systolic blood pressure", sbp.Name);
            Assert.Equal(ClinicalValueType.Numeric, sbp.ValueType);
            Assert.Equal(ClinicalVariableCategory.Clinical, sbp.Category);
            Assert.True(sbp.IsRequired);
            Assert.Equal("mmHg", sbp.CanonicalUnit);

            var female = Assert.Single(result.Data.Items, x => x.Code == "46098-0");
            Assert.Equal(ClinicalValueType.Boolean, female.ValueType);
            Assert.Equal(ClinicalVariableCategory.PersonalInformation, female.Category);

            var sarsCov2 = Assert.Single(result.Data.Items, x => x.Code == "94500-6");
            Assert.Equal(ClinicalValueType.Categorical, sarsCov2.ValueType);
            // A boolean / categorical variable has no canonical unit
            Assert.Null(sarsCov2.CanonicalUnit);
        }

        [Fact]
        public async Task GetPagedClinicalVariable_ExcludesSoftDeleted_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 100 },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.DoesNotContain(result.Data.Items, x => x.Name == "Body temperature");
        }

        /*=== filters ===*/

        [Fact]
        public async Task GetPagedClinicalVariable_NameFilter_CaseInsensitivePartialMatch_Success()
        {
            await SeedAsync();

            // Uppercase pattern against the stored "Blood glucose" proves ILike
            // case-insensitivity, and only the fragment has to match
            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new ClinicalVariableFilter { Name = "GLUCOSE" },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Blood glucose", item.Name);
            Assert.Equal(1, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedClinicalVariable_NameFilter_PartialFragment_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new ClinicalVariableFilter { Name = "react" },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("C reactive protein", item.Name);
        }

        [Fact]
        public async Task GetPagedClinicalVariable_CodeFilter_ExactMatch_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new ClinicalVariableFilter { Code = "8480-6" },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Systolic blood pressure", item.Name);
            Assert.Equal(1, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedClinicalVariable_CodeFilter_PartialCodeDoesNotMatch_Success()
        {
            await SeedAsync();

            // Business rule: the code filter is an equality match, not a prefix match
            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new ClinicalVariableFilter { Code = "8480" },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedClinicalVariable_IsRequiredFilter_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new ClinicalVariableFilter { IsRequired = true },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(
            [
                "Systolic blood pressure",
                "Female sex",
                "Blood glucose",
            ], [.. result.Data.Items.Select(x => x.Name)]);
        }

        [Fact]
        public async Task GetPagedClinicalVariable_IsRequiredFalseFilter_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new ClinicalVariableFilter { IsRequired = false },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(
            [
                "SARS-CoV-2 RNA",
                "C reactive protein",
            ], [.. result.Data.Items.Select(x => x.Name)]);
        }

        // Business rule: the value type filter maps onto the TPH discriminator, so it
        // has to return exactly the variables of that subtype
        public static readonly TheoryData<ClinicalValueType> ValueTypes =
        [
            ClinicalValueType.Numeric,
            ClinicalValueType.Boolean,
            ClinicalValueType.Categorical,
        ];

        [Theory]
        [MemberData(nameof(ValueTypes))]
        public async Task GetPagedClinicalVariable_ValueTypeFilter_ReturnsOnlyThatSubtype_Success(
            ClinicalValueType valueType)
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new ClinicalVariableFilter { ValueType = valueType },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.All(result.Data.Items, x => Assert.Equal(valueType, x.ValueType));
            Assert.Equal(valueType switch
            {
                ClinicalValueType.Numeric => 3,
                ClinicalValueType.Boolean => 1,
                ClinicalValueType.Categorical => 1,
                _ => 0,
            }, result.Data.Metadata.TotalItemCount);
        }

        public static readonly TheoryData<ClinicalVariableCategory> Categories =
        [
            ClinicalVariableCategory.Clinical,
            ClinicalVariableCategory.Paraclinical,
            ClinicalVariableCategory.PersonalInformation,
        ];

        [Theory]
        [MemberData(nameof(Categories))]
        public async Task GetPagedClinicalVariable_CategoryFilter_ReturnsOnlyThatCategory_Success(
            ClinicalVariableCategory category)
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new ClinicalVariableFilter { Category = category },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.All(result.Data.Items, x => Assert.Equal(category, x.Category));
            Assert.Equal(category switch
            {
                ClinicalVariableCategory.Clinical => 1,
                ClinicalVariableCategory.Paraclinical => 3,
                ClinicalVariableCategory.PersonalInformation => 1,
                _ => 0,
            }, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedClinicalVariable_CombinedFilters_AppliedTogether_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new ClinicalVariableFilter
                {
                    ValueType = ClinicalValueType.Numeric,
                    IsRequired = true,
                },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(
            [
                "Systolic blood pressure",
                "Blood glucose",
            ], [.. result.Data.Items.Select(x => x.Name)]);
        }

        [Fact]
        public async Task GetPagedClinicalVariable_FilterMatchesNothing_ReturnsEmpty()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new ClinicalVariableFilter { Name = "Vancomycin" },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedClinicalVariable_FilterWithoutCriteria_AppliesNoFilter_Success()
        {
            // Boundary: a filter object where every criterion is null must not restrict anything
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new ClinicalVariableFilter(),
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedClinicalVariable_CategoryFilterNotDefined_ReturnsNoMatch_Success()
        {
            // Boundary: 999 is outside every defined ClinicalVariableCategory member, so
            // no stored variable can ever satisfy the filter
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new ClinicalVariableFilter
                {
                    Category = (ClinicalVariableCategory)999,
                },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedClinicalVariable_ValueTypeFilterNotDefined_ReturnsNoMatch_Success()
        {
            // Boundary: 999 is outside every defined ClinicalValueType member, so no
            // stored variable can ever satisfy the filter
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new ClinicalVariableFilter
                {
                    ValueType = (ClinicalValueType)999,
                },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        /*=== boundary: no data at all ===*/

        [Fact]
        public async Task GetPagedClinicalVariable_EmptyDatabase_ReturnsEmpty()
        {
            var result = await _handler.HandleAsync(new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
            Assert.False(result.Data.Metadata.HasNextPage);
        }

        #endregion

        private static NumericClinicalVariable CreateNumeric(
            string name,
            string code,
            string description,
            ClinicalVariableCategory category,
            string unit,
            decimal acceptedMax,
            bool isRequired,
            DateTimeOffset createdAt,
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
                CreatedAt = createdAt,
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
    }
}
