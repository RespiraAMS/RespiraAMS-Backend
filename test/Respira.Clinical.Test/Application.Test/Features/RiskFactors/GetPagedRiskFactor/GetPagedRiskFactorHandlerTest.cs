using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.RiskFactors.GetPagedRiskFactor;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.Clinical.Infrastructure.Mapper;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.RiskFactors.GetPagedRiskFactor
{
    public class GetPagedRiskFactorHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly GetPagedRiskFactorHandler _handler;
        private readonly IDbContext _context;

        public GetPagedRiskFactorHandlerTest(PostgresFixture fixture)
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
            // Clear leftover data (children first for FK constraints) so the ordering /
            // count assertions are deterministic across runs. IgnoreQueryFilters because
            // soft deleted rows are hidden by the query filter but still occupy the table
            await _context.MetricsRules.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.RiskFactors.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Treatments.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.ClinicalMetrics.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Pathogens.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Criteria.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        private sealed record SeedData(
            Guid Klebsiella,
            Guid Pneumococcus,
            Guid Pseudomonas,
            Dictionary<string, RiskFactor> ByCriterionName);

        // The thresholds are the real clinical decision points: CURB-65 age, hypotension,
        // tachypnea, severe sepsis tachycardia and the fever cut off
        private static readonly (string Name, string Code, decimal Threshold, ExpressionOperator Op)[] CriteriaSeed =
        [
            ("Tuổi >= 65", "AGE", 65, ExpressionOperator.GTE),
            ("Huyết áp tâm thu < 90 mmHg", "SBP", 90, ExpressionOperator.LT),
            ("Số lần thở >= 30 lần/phút", "RR", 30, ExpressionOperator.GTE),
            ("Nhịp tim >= 125 lần/phút", "HR", 125, ExpressionOperator.GTE),
            ("Thân nhiệt > 38 °C", "TEMP", 38, ExpressionOperator.GT),
        ];

        // Newest first, which is deliberately not the alphabetical order of the names
        private static readonly List<string> ExpectedOrder =
        [
            "Tuổi >= 65",
            "Huyết áp tâm thu < 90 mmHg",
            "Số lần thở >= 30 lần/phút",
            "Nhịp tim >= 125 lần/phút",
            "Thân nhiệt > 38 °C",
        ];

        private static readonly List<string> ExpectedFormulas =
        [
            "AGE ≥ 65",
            "SBP < 90",
            "RR ≥ 30",
            "HR ≥ 125",
            "TEMP > 38",
        ];

        /*
         * Seeds five live risk factors whose CreatedAt are spaced 1 minute apart so the
         * CreatedAt-descending order of the handler is deterministic, plus one soft
         * deleted factor that must never show up. The factors are spread over three
         * pathogens so the PathogenId filter has both matching and non matching rows
         *
         * Newest first: Klebsiella/Tuổi, Pneumococcus/Huyết áp, Klebsiella/Số lần thở,
         *               Pseudomonas/Nhịp tim, Pseudomonas/Thân nhiệt
         */
        private async Task<SeedData> SeedAsync()
        {
            var baseTime = DateTimeOffset.UtcNow;

            var klebsiella = new Pathogen
            {
                Name = "Klebsiella pneumoniae",
                Description = "Gram-negative bacillus causing hospital-acquired pneumonia",
                IsAtypical = true,
            };
            var pneumococcus = new Pathogen
            {
                Name = "Streptococcus pneumoniae",
                Description = "Gram-positive diplococcus, the most common community acquired pathogen",
                IsAtypical = false,
            };
            var pseudomonas = new Pathogen
            {
                Name = "Pseudomonas aeruginosa",
                Description = "Gram-negative opportunist, often multidrug resistant",
                IsAtypical = true,
            };
            await _context.Pathogens.AddRangeAsync(
                [klebsiella, pneumococcus, pseudomonas], TestContext.Current.CancellationToken);

            var criteria = CriteriaSeed
                .Select(x => new Criterion(x.Name, new BinaryFormula(
                    new VariableFormula(new VariableRef(Guid.CreateVersion7(), x.Code, ClinicalValueType.Numeric)),
                    new NumericConstantFormula(x.Threshold),
                    x.Op)))
                .ToList();
            await _context.Criteria.AddRangeAsync(criteria, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var byName = criteria.ToDictionary(x => x.Name, x => x);
            var pathogenByCriterion = new[]
            {
                klebsiella.Id,
                pneumococcus.Id,
                klebsiella.Id,
                pseudomonas.Id,
                pseudomonas.Id,
            };

            var factors = new List<RiskFactor>();
            for (var i = 0; i < criteria.Count; i++)
            {
                factors.Add(new RiskFactor
                {
                    PathogenId = pathogenByCriterion[i],
                    CriterionId = criteria[i].Id,
                    CreatedAt = baseTime.AddMinutes(-i),
                });
            }

            // Soft deleted: hidden by the query filter but still in the table, and the
            // newest row so any leak would show up first
            factors.Add(new RiskFactor
            {
                PathogenId = pneumococcus.Id,
                CriterionId = criteria[0].Id,
                CreatedAt = baseTime.AddMinutes(1),
                IsDeleted = true,
                DeletedAt = DateTimeOffset.UtcNow,
            });

            await _context.RiskFactors.AddRangeAsync(factors, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var byCriterionName = new Dictionary<string, RiskFactor>();
            for (var i = 0; i < criteria.Count; i++)
            {
                byCriterionName[criteria[i].Name] = factors[i];
            }

            return new SeedData(klebsiella.Id, pneumococcus.Id, pseudomonas.Id, byCriterionName);
        }

        private static GetPagedRiskFactorQuery Query(int page, int size, RiskFactorFilter? filter = null) => new()
        {
            Param = new PaginationParam { Page = page, Size = size },
            Filter = filter,
        };

        #region Happy path

        [Fact]
        public async Task GetPagedRiskFactor_FirstPageNewestFirst_Success()
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
                "Tuổi >= 65",
                "Huyết áp tâm thu < 90 mmHg",
            ], [.. result.Data.Items.Select(x => x.Criterion.Name)]);

            Assert.Equal(1, result.Data.Metadata.CurrentPage);
            Assert.Equal(2, result.Data.Metadata.PageSize);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.Equal(3, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasPreviousPage);
            Assert.True(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task GetPagedRiskFactor_LastPartialPage_HasNoNext_Success()
        {
            // Upper boundary page: only 1 leftover item and no next page
            await SeedAsync();

            var result = await _handler.HandleAsync(Query(page: 3, size: 2),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Thân nhiệt > 38 °C", item.Criterion.Name);
            Assert.True(result.Data.Metadata.HasPreviousPage);
            Assert.False(result.Data.Metadata.HasNextPage);
            Assert.Equal(3, result.Data.Metadata.CurrentPage);
        }

        [Fact]
        public async Task GetPagedRiskFactor_PageBeyondRange_ReturnsNoItems()
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
         * and size 100 the largest, so the same five risk factors must come back either
         * one at a time or all together
         */
        [Fact]
        public async Task GetPagedRiskFactor_MinimumPageSize_WalksOneItemPerPage_Success()
        {
            await SeedAsync();

            var first = await _handler.HandleAsync(Query(page: 1, size: 1),
                TestContext.Current.CancellationToken);
            Assert.True(first.IsSuccess());
            Assert.NotNull(first.Data);
            Assert.Equal("Tuổi >= 65", Assert.Single(first.Data.Items).Criterion.Name);
            Assert.True(first.Data.Metadata.HasNextPage);
            Assert.False(first.Data.Metadata.HasPreviousPage);

            // The oldest live risk factor sits on the 5th and last page
            var last = await _handler.HandleAsync(Query(page: 5, size: 1),
                TestContext.Current.CancellationToken);
            Assert.True(last.IsSuccess());
            Assert.NotNull(last.Data);
            Assert.Equal("Thân nhiệt > 38 °C", Assert.Single(last.Data.Items).Criterion.Name);
            Assert.True(last.Data.Metadata.HasPreviousPage);
            Assert.False(last.Data.Metadata.HasNextPage);

            var beyond = await _handler.HandleAsync(Query(page: 6, size: 1),
                TestContext.Current.CancellationToken);
            Assert.True(beyond.IsSuccess());
            Assert.NotNull(beyond.Data);
            Assert.Empty(beyond.Data.Items);
        }

        [Fact]
        public async Task GetPagedRiskFactor_MaxPageSize_AllItemsFitOnFirstPage_Success()
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
        public async Task GetPagedRiskFactor_ProjectsPathogenAndCriterionAndFormula_Success()
        {
            var seeded = await SeedAsync();

            var result = await _handler.HandleAsync(Query(page: 1, size: 100),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            // Every item carries the joined pathogen, the joined criterion and the
            // criterion formula rendered back from its JSONB payload
            Assert.Equal(ExpectedOrder, [.. result.Data.Items.Select(x => x.Criterion.Name)]);
            Assert.Equal(ExpectedFormulas, [.. result.Data.Items.Select(x => x.Criterion.Formula)]);
            Assert.Equal(
            [
                "Klebsiella pneumoniae",
                "Streptococcus pneumoniae",
                "Klebsiella pneumoniae",
                "Pseudomonas aeruginosa",
                "Pseudomonas aeruginosa",
            ], [.. result.Data.Items.Select(x => x.Pathogen.Name)]);

            Assert.All(result.Data.Items, x => Assert.NotEqual(Guid.Empty, x.Id));
            Assert.Equal(
                seeded.ByCriterionName.Values.Select(x => x.Id).Order().ToArray(),
                result.Data.Items.Select(x => x.Id).Order().ToArray());

            // The joined ids are the real rows, not copies
            var first = result.Data.Items.First();
            Assert.Equal(seeded.Klebsiella, first.Pathogen.Id);
            Assert.Equal(seeded.ByCriterionName["Tuổi >= 65"].CriterionId, first.Criterion.Id);
        }

        [Fact]
        public async Task GetPagedRiskFactor_ExcludesSoftDeleted_Success()
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
        public async Task GetPagedRiskFactor_PathogenFilter_ReturnsOnlyThatPathogen_Success()
        {
            var seeded = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(page: 1, size: 10, new RiskFactorFilter { PathogenId = seeded.Klebsiella }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(2, result.Data.Metadata.TotalItemCount);
            Assert.Equal(
            [
                "Tuổi >= 65",
                "Số lần thở >= 30 lần/phút",
            ], [.. result.Data.Items.Select(x => x.Criterion.Name)]);
            Assert.All(result.Data.Items, x => Assert.Equal(seeded.Klebsiella, x.Pathogen.Id));
        }

        [Fact]
        public async Task GetPagedRiskFactor_PathogenFilter_NoMatch_ReturnsEmpty()
        {
            await SeedAsync();

            // A pathogen that exists nowhere cannot own a risk factor
            var result = await _handler.HandleAsync(
                Query(page: 1, size: 10, new RiskFactorFilter { PathogenId = Guid.CreateVersion7() }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedRiskFactor_FilterWithoutPathogenId_AppliesNoFilter_Success()
        {
            // Boundary: a filter object whose only criterion is null must not restrict
            // anything, which is the same as sending no filter at all
            await SeedAsync();

            var withEmptyFilter = await _handler.HandleAsync(
                Query(page: 1, size: 100, new RiskFactorFilter()),
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

        [Fact]
        public async Task GetPagedRiskFactor_PathogenFilter_HidesSoftDeletedRows_Success()
        {
            /*
             * The soft deleted factor of the seeded set belongs to the pneumococcus, so
             * the filter must count only the live one
             */
            var seeded = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(page: 1, size: 10, new RiskFactorFilter { PathogenId = seeded.Pneumococcus }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(1, result.Data.Metadata.TotalItemCount);
            Assert.Equal("Huyết áp tâm thu < 90 mmHg", Assert.Single(result.Data.Items).Criterion.Name);
        }

        /*=== boundary: no data at all ===*/

        [Fact]
        public async Task GetPagedRiskFactor_EmptyDatabase_ReturnsEmptySuccess()
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
         * Fail path: GetPagedRiskFactorHandler has no failure branch. The pagination is
         * validated by GetPagedRiskFactorValidator before the handler runs, the filter
         * can only narrow the result set and the projection cannot fail, so the only
         * failure a database outage would produce is not a behaviour of this feature
         */
    }
}
