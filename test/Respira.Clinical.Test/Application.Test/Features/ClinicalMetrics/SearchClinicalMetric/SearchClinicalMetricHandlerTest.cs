using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.ClinicalMetrics.SearchClinicalMetric;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Infrastructure.Data;
using Respira.Clinical.Infrastructure.Mapper;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.ClinicalMetrics.SearchClinicalMetric
{
    public class SearchClinicalMetricHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly SearchClinicalMetricHandler _handler;
        private readonly IDbContext _context;

        public SearchClinicalMetricHandlerTest(PostgresFixture fixture)
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
            // Rules reference metrics through a FK, so delete them first. IgnoreQueryFilters
            // is needed because soft-deleted rows are hidden by the query filter but still
            // occupy the table
            await _context.MetricRules.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.ClinicalMetrics.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        private async Task<ClinicalMetric> SeedMetricAsync(
            string name, string code, string description, string version, string publisher,
            bool isApplied, bool softDeleted = false)
        {
            var metric = new ClinicalMetric
            {
                Name = name,
                Code = code,
                Description = description,
                Version = version,
                Publisher = publisher,
                IsApplied = isApplied,
                Rules = [],
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.ClinicalMetrics.AddAsync(metric, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return metric;
        }

        // Five realistic pneumonia severity metrics. Every description or name contains
        // the word "metrics", so the term "metric" matches all of them
        private static readonly (string Name, string Code, string Description, string Version, string Publisher, bool IsApplied)[] MetricsSeed =
        [
            ("ICU hospitalization criteria by IDSA/ATS", "IDSA/ATS",
                "IDSA/ATS metrics to assess whether a severe patient need ICU or not",
                "2026", "Hiệp hội Bệnh nhiễm trùng Mỹ (IDSA) và Hiệp hội Lồng ngực Mỹ (ATS)", true),
            ("4C Mortality Score", "4C",
                "ISARIC4C mortality metrics for adults admitted with COVID-19",
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

        /*
         * Seeds the five live pneumonia metrics plus a soft-deleted copy of the PSI
         * metric that shares its code, so the query filter has something to hide
         */
        private async Task<IReadOnlyList<ClinicalMetric>> SeedRealisticMetricsAsync()
        {
            var seeded = new List<ClinicalMetric>();
            foreach (var seed in MetricsSeed)
            {
                seeded.Add(await SeedMetricAsync(seed.Name, seed.Code, seed.Description,
                    seed.Version, seed.Publisher, seed.IsApplied));
            }

            await SeedMetricAsync("Pneumonia Severity Index (PSI, bản lưu trữ)", "PSI",
                "Archived copy of the PSI metrics kept for the revision history", "1997",
                "Đội ngũ Nghiên cứu Kết quả Điều trị Viêm phổi (PORT - Pneumonia Outcomes Research Team)",
                true, softDeleted: true);

            return seeded;
        }

        #region Happy path

        [Fact]
        public async Task SearchClinicalMetric_PrefixMatchOnName_ReturnsFullProjection_Success()
        {
            await SeedRealisticMetricsAsync();

            var result = await _handler.HandleAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "idsa",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            // "idsa" is a prefix of the stored lexeme "idsa" that appears in the name,
            // the code and the description of this single metric
            var item = Assert.Single(result.Data.Items);
            Assert.Equal("ICU hospitalization criteria by IDSA/ATS", item.Name);
            Assert.Equal("IDSA/ATS", item.Code);
            Assert.Equal("IDSA/ATS metrics to assess whether a severe patient need ICU or not", item.Description);
            Assert.Equal("2026", item.Version);
            Assert.Equal("Hiệp hội Bệnh nhiễm trùng Mỹ (IDSA) và Hiệp hội Lồng ngực Mỹ (ATS)", item.Publisher);
            Assert.True(item.IsApplied);
            Assert.True(item.Rank > 0);

            Assert.Equal(1, result.Data.Metadata.TotalItemCount);
            Assert.Equal(1, result.Data.Metadata.CurrentPage);
        }

        [Theory]
        [InlineData("psi")]  // full code
        [InlineData("PSI")]  // the respira_vi configuration lowercases both sides
        [InlineData("ps")]   // shorter prefix still matches
        public async Task SearchClinicalMetric_PrefixTerm_MatchesSameMetric_Success(string searchTerm)
        {
            await SeedRealisticMetricsAsync();

            var result = await _handler.HandleAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            // Only the live PSI metric matches, not the soft-deleted archived copy
            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Pneumonia Severity Index (PSI)", item.Name);
            Assert.True(item.IsApplied);
        }

        [Fact]
        public async Task SearchClinicalMetric_MatchOnlyInDescription_ReturnsMetric_Success()
        {
            await SeedRealisticMetricsAsync();

            // "blood urea" only appears together in the CRB-65 description, so the
            // multi word AND query narrows the result to a single metric
            var result = await _handler.HandleAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "blood urea",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Severity metrics CRB-65", item.Name);
            Assert.False(item.IsApplied);
            Assert.True(item.Rank > 0);
        }

        [Theory]
        [InlineData("diem")]  // unaccented query term
        [InlineData("điểm")]  // accented query term
        public async Task SearchClinicalMetric_AccentFoldedTerm_MatchesVietnameseData_Success(string searchTerm)
        {
            await SeedMetricAsync("Thang điểm đánh giá độ nặng", "TD-01",
                "Thang điểm nội bộ để đánh giá độ nặng của bệnh nhân viêm phổi",
                "2024", "Bệnh viện Đại học Y Dược", true);

            // Both the stored vector and the query run through the respira_vi
            // configuration, which folds accents with unaccent
            var result = await _handler.HandleAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Thang điểm đánh giá độ nặng", item.Name);
        }

        [Fact]
        public async Task SearchClinicalMetric_NameMatchOutranksDescriptionMatch_Success()
        {
            await SeedRealisticMetricsAsync();

            // "metric" appears in the name of CURB-65 and CRB-65 (weight A) and in the
            // description of the other three (weight C)
            var result = await _handler.HandleAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "metric",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);

            var ordered = result.Data.Items.ToList();
            // Name matches (weight A) come first, then the description-only matches (weight C)
            Assert.All(ordered.Take(2), x => Assert.Contains("Severity metrics", x.Name));
            Assert.All(ordered.Skip(2), x => Assert.DoesNotContain("Severity metrics", x.Name));
            Assert.True(ordered[1].Rank > ordered[2].Rank);
        }

        [Fact]
        public async Task SearchClinicalMetric_SameTermInDifferentFields_RanksNameAboveCodeAboveDescription_Success()
        {
            // A single term placed once in each searched field: name (weight A),
            // code (weight B) and description (weight C)
            await SeedMetricAsync("SOFA assessment", "SF01",
                "Organ dysfunction score for ICU patients", "2016", "Society of Critical Care Medicine", true);
            await SeedMetricAsync("Organ dysfunction score", "SOFA-T2",
                "Organ dysfunction score for ICU patients", "2016", "Society of Critical Care Medicine", false);
            await SeedMetricAsync("Organ dysfunction score variant", "SF03",
                "Derived from the SOFA score for triage", "2016", "Society of Critical Care Medicine", true);

            var result = await _handler.HandleAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "sofa",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var ordered = result.Data.Items.ToList();
            Assert.Equal(
            [
                "SOFA assessment",                  // name match
                "Organ dysfunction score",          // code match
                "Organ dysfunction score variant",  // description match
            ], [.. ordered.Select(x => x.Name)]);
            Assert.True(ordered[0].Rank > ordered[1].Rank);
            Assert.True(ordered[1].Rank > ordered[2].Rank);
        }

        /*=== pagination ===*/

        [Fact]
        public async Task SearchClinicalMetric_FirstPage_SplitsMatchesWithMetadata_Success()
        {
            // 5 matches with page size 2 -> pages of [2, 2, 1]
            await SeedRealisticMetricsAsync();

            var result = await _handler.HandleAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = 1, Size = 2 },
                SearchTerm = "metric",
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
        public async Task SearchClinicalMetric_LastPage_HasNoNext_Success()
        {
            // Upper boundary page: the single leftover match, no next page
            await SeedRealisticMetricsAsync();

            var result = await _handler.HandleAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = 3, Size = 2 },
                SearchTerm = "metric",
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
        public async Task SearchClinicalMetric_MinimumPageSize_ReturnsOneItemPerPage_Success()
        {
            // Lower boundary of a usable page size
            await SeedRealisticMetricsAsync();

            var result = await _handler.HandleAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = 1, Size = 1 },
                SearchTerm = "metric",
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
        public async Task SearchClinicalMetric_MaximumPageSize_ReturnsAllMatchesInOnePage_Success()
        {
            // Upper boundary of a usable page size
            await SeedRealisticMetricsAsync();

            var result = await _handler.HandleAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = 1, Size = 100 },
                SearchTerm = "metric",
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
        public async Task SearchClinicalMetric_NoUsableLexeme_ReturnsEmptyPage_Success(string searchTerm)
        {
            await SeedRealisticMetricsAsync();

            var result = await _handler.HandleAsync(new SearchClinicalMetricQuery
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
        public async Task SearchClinicalMetric_TermMatchesNothing_ReturnsEmptyPage_Success()
        {
            await SeedRealisticMetricsAsync();

            // "qsofa" is a real clinical metric name but is not seeded here
            var result = await _handler.HandleAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "qsofa",
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
        public async Task SearchClinicalMetric_TermIsInsideNameButNotPrefix_ReturnsEmptyPage_Success()
        {
            // "tality" appears inside "Mortality" but matching is by prefix, so a term
            // that is only a suffix or an infix must not match
            await SeedRealisticMetricsAsync();

            var result = await _handler.HandleAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "tality",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        #endregion

        #region Soft-deleted clinical metric

        [Fact]
        public async Task SearchClinicalMetric_SoftDeletedMetric_IsExcluded_Success()
        {
            // Both the live PSI metric and a soft-deleted copy share the code "PSI".
            // Only the live one may be returned
            var seeded = await SeedRealisticMetricsAsync();
            var livePsi = seeded.Single(x => x.Code == "PSI");

            var result = await _handler.HandleAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "psi",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal(livePsi.Id, item.Id);
            Assert.Equal(1, result.Data.Metadata.TotalItemCount);
        }

        #endregion

        #region Symbol-only search term

        /*
         * FullTextSearchUtil.BuildPrefixQuery sanitises every character that is not a
         * letter, a digit or a hyphen, so a symbol-only term produces an empty tsquery
         * and the handler short-circuits to an empty page instead of sending malformed
         * tsquery syntax (& | ! ( ) : *) to PostgreSQL.
         *
         * These terms are also rejected by SearchClinicalMetricValidator, so in the
         * normal pipeline this is defence in depth for the shared utility.
         */
        [Theory]
        [InlineData("&&")]
        [InlineData("||")]
        [InlineData("!!")]
        [InlineData("((")]
        public async Task SearchClinicalMetric_SymbolOnlyTerm_ReturnsEmptyPage_Success(string searchTerm)
        {
            await SeedRealisticMetricsAsync();

            var result = await _handler.HandleAsync(new SearchClinicalMetricQuery
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
         * Fail path: SearchClinicalMetricHandler has no failure branch. It only ever
         * returns a success result (an empty page when nothing matches or the term has
         * no usable lexeme). Input validation happens earlier in
         * SearchClinicalMetricValidator, and the full text query cannot fail for
         * sanitised input, so there is no reachable failure result to assert here
         */
    }
}
