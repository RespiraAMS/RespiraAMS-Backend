using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.ClinicalMetrics.GetPagedClinicalMetric;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Infrastructure.Data;
using Respira.Clinical.Infrastructure.Mapper;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.ClinicalMetrics.GetPagedClinicalMetric
{
    public class GetPagedClinicalMetricHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly GetPagedClinicalMetricHandler _handler;
        private readonly IDbContext _context;

        public GetPagedClinicalMetricHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
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
            // across runs. IgnoreQueryFilters because soft deleted rows are hidden by
            // the query filter but still occupy the table
            await _context.MetricRules.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.ClinicalMetrics.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        private sealed record SeedData(Dictionary<string, Guid> IdByCode, Guid ArchivedId);

        // Three of the metrics are in use, the CRB-65 variant and the 4C score are not,
        // so the IsApplied filter has both matching and non matching rows
        private static readonly (string Name, string Code, string Description, string Version, string Publisher, bool IsApplied)[] MetricsSeed =
        [
            ("ICU hospitalization criteria by IDSA/ATS", "IDSA/ATS",
                "IDSA/ATS metrics to assess whether a severe patient need ICU or not",
                "2026", "Hiệp hội Bệnh nhiễm trùng Mỹ (IDSA) và Hiệp hội Lồng ngực Mỹ (ATS)", true),
            ("4C Mortality Score", "4C",
                "ISARIC4C mortality score for adults admitted with COVID-19",
                "2021", "ISARIC (International Severe Acute Respiratory and Emerging Infection Consortium)", false),
            ("Severity metrics CURB-65", "CURB-65",
                "CURB-65 metrics to assess severity of a patient",
                "2002-2003", "Hiệp hội Lồng ngực Anh (BTS)", true),
            ("Severity metrics CRB-65", "CRB-65",
                "CRB-65 metrics to assess severity of a patient without the blood urea nitrogen component",
                "2002-2003", "Hiệp hội Lồng ngực Anh (BTS)", false),
            ("Pneumonia Severity Index (PSI)", "PSI",
                "PSI metrics to assess severity of a patient with more detail than CURB-65",
                "1997", "Đội ngũ Nghiên cứu Kết quả Điều trị Viêm phổi (PORT - Pneumonia Outcomes Research Team)", true),
        ];

        // Newest first, which is deliberately neither the alphabetical order of the
        // codes nor the publication order of the versions
        private static readonly List<string> ExpectedOrder =
        [
            "ICU hospitalization criteria by IDSA/ATS",
            "4C Mortality Score",
            "Severity metrics CURB-65",
            "Severity metrics CRB-65",
            "Pneumonia Severity Index (PSI)",
        ];

        /*
         * Seeds five live metrics whose CreatedAt are spaced 1 minute apart so the
         * CreatedAt-descending order of the handler is deterministic, plus one soft
         * deleted copy that must never show up. The copy deliberately shares the PSI
         * code, so the code filter has to count only the live PSI metric
         */
        private async Task<SeedData> SeedAsync()
        {
            var baseTime = DateTimeOffset.UtcNow;
            var idByCode = new Dictionary<string, Guid>();

            for (var i = 0; i < MetricsSeed.Length; i++)
            {
                var seed = MetricsSeed[i];
                var metric = new ClinicalMetric(
                    seed.Name, seed.Code, seed.Description, seed.Version, seed.Publisher, seed.IsApplied, [])
                {
                    CreatedAt = baseTime.AddMinutes(-i),
                    UpdatedAt = baseTime.AddMinutes(-i),
                };
                await _context.ClinicalMetrics.AddAsync(metric, TestContext.Current.CancellationToken);
                await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
                idByCode[seed.Code] = metric.Id;
            }

            // Soft deleted: hidden by the query filter but still in the table, and the
            // newest row so any leak would show up first
            var archived = new ClinicalMetric(
                "Pneumonia Severity Index (PSI, bản lưu trữ)",
                "PSI",
                "Archived copy of the PSI metric kept for the revision history",
                "1997",
                "Đội ngũ Nghiên cứu Kết quả Điều trị Viêm phổi (PORT - Pneumonia Outcomes Research Team)",
                true,
                [])
            {
                CreatedAt = baseTime.AddMinutes(1),
                UpdatedAt = baseTime.AddMinutes(1),
                IsDeleted = true,
                DeletedAt = DateTimeOffset.UtcNow,
            };
            await _context.ClinicalMetrics.AddAsync(archived, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            return new SeedData(idByCode, archived.Id);
        }

        private static GetPagedClinicalMetricQuery Query(int page, int size, ClinicalMetricFilter? filter = null) => new()
        {
            Param = new PaginationParam { Page = page, Size = size },
            Filter = filter,
        };

        #region Happy path

        [Fact]
        public async Task GetPagedClinicalMetric_FirstPageNewestFirst_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(Query(page: 1, size: 2),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            // Business rule: the browse endpoint is ordered by CreatedAt descending
            Assert.Equal(
            [
                "ICU hospitalization criteria by IDSA/ATS",
                "4C Mortality Score",
            ], [.. result.Data.Items.Select(x => x.Name)]);

            Assert.Equal(1, result.Data.Metadata.CurrentPage);
            Assert.Equal(2, result.Data.Metadata.PageSize);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.Equal(3, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasPreviousPage);
            Assert.True(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task GetPagedClinicalMetric_LastPartialPage_HasNoNext_Success()
        {
            // Upper boundary page: only 1 leftover item and no next page
            await SeedAsync();

            var result = await _handler.HandleAsync(Query(page: 3, size: 2),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Pneumonia Severity Index (PSI)", item.Name);
            Assert.True(result.Data.Metadata.HasPreviousPage);
            Assert.False(result.Data.Metadata.HasNextPage);
            Assert.Equal(3, result.Data.Metadata.CurrentPage);
        }

        [Fact]
        public async Task GetPagedClinicalMetric_PageBeyondRange_ReturnsNoItems()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(Query(page: 4, size: 2),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.False(result.Data.Metadata.HasNextPage);
        }

        /*
         * Boundary values of the pagination: size 1 is the smallest allowed page size
         * and size 100 the largest, so the same five metrics must come back either one
         * at a time or all together
         */
        [Fact]
        public async Task GetPagedClinicalMetric_MinimumPageSize_WalksOneItemPerPage_Success()
        {
            await SeedAsync();

            var first = await _handler.HandleAsync(Query(page: 1, size: 1),
                TestContext.Current.CancellationToken);
            Assert.True(first.IsSuccess());
            Assert.NotNull(first.Data);
            Assert.Equal("ICU hospitalization criteria by IDSA/ATS", Assert.Single(first.Data.Items).Name);
            Assert.True(first.Data.Metadata.HasNextPage);
            Assert.False(first.Data.Metadata.HasPreviousPage);

            // The oldest live metric sits on the 5th and last page
            var last = await _handler.HandleAsync(Query(page: 5, size: 1),
                TestContext.Current.CancellationToken);
            Assert.True(last.IsSuccess());
            Assert.NotNull(last.Data);
            Assert.Equal("Pneumonia Severity Index (PSI)", Assert.Single(last.Data.Items).Name);
            Assert.True(last.Data.Metadata.HasPreviousPage);
            Assert.False(last.Data.Metadata.HasNextPage);

            var beyond = await _handler.HandleAsync(Query(page: 6, size: 1),
                TestContext.Current.CancellationToken);
            Assert.True(beyond.IsSuccess());
            Assert.NotNull(beyond.Data);
            Assert.Empty(beyond.Data.Items);
        }

        [Fact]
        public async Task GetPagedClinicalMetric_MaxPageSize_AllItemsFitOnFirstPage_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(Query(page: 1, size: 100),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(5, result.Data.Items.Count());
            Assert.Equal(1, result.Data.Metadata.PageCount);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.False(result.Data.Metadata.HasPreviousPage);
            Assert.False(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task GetPagedClinicalMetric_ProjectsEveryField_Success()
        {
            var seeded = await SeedAsync();

            var result = await _handler.HandleAsync(Query(page: 1, size: 100),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            // Business rule: the browse item carries the whole metric payload, so the
            // client can tell the metrics apart without a second call
            Assert.Equal(ExpectedOrder, [.. result.Data.Items.Select(x => x.Name)]);

            foreach (var seed in MetricsSeed)
            {
                var item = result.Data.Items.Single(x => x.Code == seed.Code);
                Assert.Equal(seeded.IdByCode[seed.Code], item.Id);
                Assert.Equal(seed.Code, item.Code);
                Assert.Equal(seed.Name, item.Name);
                Assert.Equal(seed.Description, item.Description);
                Assert.Equal(seed.Version, item.Version);
                Assert.Equal(seed.Publisher, item.Publisher);
                Assert.Equal(seed.IsApplied, item.IsApplied);
            }

            Assert.All(result.Data.Items, x => Assert.NotEqual(Guid.Empty, x.Id));
        }

        [Fact]
        public async Task GetPagedClinicalMetric_ExcludesSoftDeleted_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(Query(page: 1, size: 100),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.Equal(5, result.Data.Items.Count());
        }

        [Fact]
        public async Task GetPagedClinicalMetric_CodeFilter_ReturnsOnlyThatMetric_Success()
        {
            var seeded = await SeedAsync();

            // The soft deleted copy also carries the PSI code, only the live one counts
            var result = await _handler.HandleAsync(
                Query(page: 1, size: 10, new ClinicalMetricFilter { Code = "PSI" }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(1, result.Data.Metadata.TotalItemCount);
            var item = Assert.Single(result.Data.Items);
            Assert.Equal(seeded.IdByCode["PSI"], item.Id);
            Assert.NotEqual(seeded.ArchivedId, item.Id);
        }

        [Fact]
        public async Task GetPagedClinicalMetric_CodeFilter_NoMatch_ReturnsEmpty()
        {
            await SeedAsync();

            // A metric code that exists nowhere cannot be listed
            var result = await _handler.HandleAsync(
                Query(page: 1, size: 10, new ClinicalMetricFilter { Code = "SMART-COP" }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedClinicalMetric_IsAppliedFilter_ReturnsOnlyInUseMetrics_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(page: 1, size: 10, new ClinicalMetricFilter { IsApplied = true }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(3, result.Data.Metadata.TotalItemCount);
            Assert.Equal(
            [
                "ICU hospitalization criteria by IDSA/ATS",
                "Severity metrics CURB-65",
                "Pneumonia Severity Index (PSI)",
            ], [.. result.Data.Items.Select(x => x.Name)]);
            Assert.All(result.Data.Items, x => Assert.True(x.IsApplied));
        }

        [Fact]
        public async Task GetPagedClinicalMetric_IsAppliedFilter_ReturnsOnlyDraftMetrics_Success()
        {
            // The other boundary of the flag: metrics that are not in use
            await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(page: 1, size: 10, new ClinicalMetricFilter { IsApplied = false }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(2, result.Data.Metadata.TotalItemCount);
            Assert.Equal(
            [
                "4C Mortality Score",
                "Severity metrics CRB-65",
            ], [.. result.Data.Items.Select(x => x.Name)]);
            Assert.All(result.Data.Items, x => Assert.False(x.IsApplied));
        }

        [Fact]
        public async Task GetPagedClinicalMetric_CodeAndIsAppliedFilters_AreCombinedWithAnd_Success()
        {
            await SeedAsync();

            // CURB-65 is in use, so only the matching combination returns the metric
            var matching = await _handler.HandleAsync(
                Query(page: 1, size: 10, new ClinicalMetricFilter { Code = "CURB-65", IsApplied = true }),
                TestContext.Current.CancellationToken);
            Assert.True(matching.IsSuccess());
            Assert.NotNull(matching.Data);
            Assert.Equal("CURB-65", Assert.Single(matching.Data.Items).Code);

            var contradicting = await _handler.HandleAsync(
                Query(page: 1, size: 10, new ClinicalMetricFilter { Code = "CURB-65", IsApplied = false }),
                TestContext.Current.CancellationToken);
            Assert.True(contradicting.IsSuccess());
            Assert.NotNull(contradicting.Data);
            Assert.Empty(contradicting.Data.Items);
            Assert.Equal(0, contradicting.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedClinicalMetric_FilterWithoutCriteria_AppliesNoFilter_Success()
        {
            // Boundary: a filter object whose only criteria are null must not restrict
            // anything, which is the same as sending no filter at all
            await SeedAsync();

            var withEmptyFilter = await _handler.HandleAsync(
                Query(page: 1, size: 100, new ClinicalMetricFilter()),
                TestContext.Current.CancellationToken);
            Assert.True(withEmptyFilter.IsSuccess());
            Assert.NotNull(withEmptyFilter.Data);
            Assert.Equal(5, withEmptyFilter.Data.Metadata.TotalItemCount);

            var withoutFilter = await _handler.HandleAsync(
                Query(page: 1, size: 100, filter: null),
                TestContext.Current.CancellationToken);
            Assert.True(withoutFilter.IsSuccess());
            Assert.NotNull(withoutFilter.Data);
            Assert.Equal(5, withoutFilter.Data.Metadata.TotalItemCount);
        }

        /*=== boundary: no data at all ===*/

        [Fact]
        public async Task GetPagedClinicalMetric_EmptyDatabase_ReturnsEmptySuccess()
        {
            var result = await _handler.HandleAsync(Query(page: 1, size: 10),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
            Assert.Equal(0, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasNextPage);
        }

        #endregion

        /*
         * Fail path: GetPagedClinicalMetricHandler has no failure branch. The pagination
         * is validated by GetPagedClinicalMetricValidator before the handler runs, both
         * filters can only narrow the result set and the projection cannot fail, so the
         * only failure a database outage would produce is not a behaviour of this feature
         */
    }
}
