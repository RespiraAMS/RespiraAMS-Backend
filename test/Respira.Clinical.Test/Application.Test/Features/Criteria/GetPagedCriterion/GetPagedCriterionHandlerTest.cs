using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Criteria.GetPagedCriterion;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.Clinical.Infrastructure.Mapper;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.Criteria.GetPagedCriterion
{
    public class GetPagedCriterionHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly GetPagedCriterionHandler _handler;
        private readonly IDbContext _context;

        public GetPagedCriterionHandlerTest(PostgresFixture fixture)
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
            await _context.Criteria.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        /*
         * Seeds five live criteria whose CreatedAt are spaced 1 minute apart so that the
         * CreatedAt-descending order of the handler is deterministic, plus one soft
         * deleted criterion that must never show up. The formulas use the real clinical
         * thresholds: CURB-65 age, fever, hypotension, tachypnea and tachycardia.
         *
         * Newest first: Tachycardia, Hypotension, Sepsis suspicion,
         *               Bacteremia risk, Fever over 38
         * This order is deliberately different from the alphabetical order used by the
         * list endpoint, so a wrong ordering column cannot pass unnoticed
         */
        private async Task<Dictionary<string, Criterion>> SeedAsync()
        {
            var baseTime = DateTimeOffset.UtcNow;
            var criteria = new List<Criterion>
            {
                Create("Tachycardia", Numeric("HR", 125, ExpressionOperator.GTE), baseTime),
                Create("Hypotension", Numeric("SBP", 90, ExpressionOperator.LT), baseTime.AddMinutes(-1)),
                Create("Sepsis suspicion", Numeric("RR", 30, ExpressionOperator.GTE), baseTime.AddMinutes(-2)),
                Create("Bacteremia risk", Numeric("AGE", 65, ExpressionOperator.GTE), baseTime.AddMinutes(-3)),
                Create("Fever over 38", Numeric("TEMP", 38, ExpressionOperator.GT), baseTime.AddMinutes(-4)),
                // Soft deleted: hidden by the query filter but still in the table
                Create(
                    "Archived screening",
                    new BooleanConstantFormula(true),
                    baseTime.AddMinutes(-5),
                    isDeleted: true),
            };

            await _context.Criteria.AddRangeAsync(criteria, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            return criteria.ToDictionary(x => x.Name, x => x);
        }

        private static Criterion Create(string name, Formula formula, DateTimeOffset createdAt, bool isDeleted = false)
        {
            return new Criterion(name, formula)
            {
                CreatedAt = createdAt,
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
        }

        private static Formula Numeric(string code, decimal threshold, ExpressionOperator op)
        {
            return new BinaryFormula(
                new VariableFormula(new VariableRef(Guid.CreateVersion7(), code, ClinicalValueType.Numeric)),
                new NumericConstantFormula(threshold),
                op);
        }

        private static GetPagedCriterionQuery Query(int page, int size) => new()
        {
            Param = new PaginationParam { Page = page, Size = size },
        };

        #region Happy path

        [Fact]
        public async Task GetPagedCriterion_FirstPageNewestFirst_Success()
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
                "Tachycardia",
                "Hypotension",
            ], [.. result.Data.Items.Select(x => x.Name)]);

            Assert.Equal(1, result.Data.Metadata.CurrentPage);
            Assert.Equal(2, result.Data.Metadata.PageSize);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.Equal(3, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasPreviousPage);
            Assert.True(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task GetPagedCriterion_LastPartialPage_HasNoNext_Success()
        {
            // Upper boundary page: only 1 leftover item and no next page
            await SeedAsync();

            var result = await _handler.HandleAsync(Query(page: 3, size: 2),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Fever over 38", item.Name);
            Assert.True(result.Data.Metadata.HasPreviousPage);
            Assert.False(result.Data.Metadata.HasNextPage);
            Assert.Equal(3, result.Data.Metadata.CurrentPage);
        }

        [Fact]
        public async Task GetPagedCriterion_PageBeyondRange_ReturnsNoItems()
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
         * Boundary values of the pagination: size 1 is the smallest allowed page size and
         * size 100 the largest, so the same five criteria must come back either one at a
         * time or all together
         */
        [Fact]
        public async Task GetPagedCriterion_MinimumPageSize_WalksOneItemPerPage_Success()
        {
            await SeedAsync();

            var first = await _handler.HandleAsync(Query(page: 1, size: 1),
                TestContext.Current.CancellationToken);
            Assert.True(first.IsSuccess());
            Assert.NotNull(first.Data);
            Assert.Equal("Tachycardia", Assert.Single(first.Data.Items).Name);
            Assert.True(first.Data.Metadata.HasNextPage);
            Assert.False(first.Data.Metadata.HasPreviousPage);

            // The oldest live criterion sits on the 5th and last page
            var last = await _handler.HandleAsync(Query(page: 5, size: 1),
                TestContext.Current.CancellationToken);
            Assert.True(last.IsSuccess());
            Assert.NotNull(last.Data);
            Assert.Equal("Fever over 38", Assert.Single(last.Data.Items).Name);
            Assert.True(last.Data.Metadata.HasPreviousPage);
            Assert.False(last.Data.Metadata.HasNextPage);

            var beyond = await _handler.HandleAsync(Query(page: 6, size: 1),
                TestContext.Current.CancellationToken);
            Assert.True(beyond.IsSuccess());
            Assert.NotNull(beyond.Data);
            Assert.Empty(beyond.Data.Items);
        }

        [Fact]
        public async Task GetPagedCriterion_MaxPageSize_AllItemsFitOnFirstPage_Success()
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
        public async Task GetPagedCriterion_ProjectsIdNameAndRenderedFormula_Success()
        {
            var seeded = await SeedAsync();

            var result = await _handler.HandleAsync(Query(page: 1, size: 100),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            // Every item carries the id, the name and the human readable formula, and
            // comes back newest first
            Assert.Equal(
            [
                "Tachycardia",
                "Hypotension",
                "Sepsis suspicion",
                "Bacteremia risk",
                "Fever over 38",
            ], [.. result.Data.Items.Select(x => x.Name)]);
            Assert.Equal(
            [
                "HR ≥ 125",
                "SBP < 90",
                "RR ≥ 30",
                "AGE ≥ 65",
                "TEMP > 38",
            ], [.. result.Data.Items.Select(x => x.Formula)]);

            // The formula is rendered from the stored JSONB payload, not from the input
            Assert.All(result.Data.Items, x => Assert.Equal(seeded[x.Name].Formula.ToString(), x.Formula));
            Assert.All(result.Data.Items, x => Assert.Equal(seeded[x.Name].Id, x.Id));
            Assert.All(result.Data.Items, x => Assert.NotEqual(Guid.Empty, x.Id));
        }

        [Fact]
        public async Task GetPagedCriterion_ExcludesSoftDeleted_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(Query(page: 1, size: 100),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.DoesNotContain(result.Data.Items, x => x.Name == "Archived screening");
        }

        [Fact]
        public async Task GetPagedCriterion_DuplicateNames_KeptAsSeparateRows_Success()
        {
            /*
             * Business rule from CreateCriterion: a criterion name is not unique, so two
             * rows may share one name and both must be paged independently
             */
            var createdAt = DateTimeOffset.UtcNow;
            await _context.Criteria.AddRangeAsync(
            [
                Create("Hypotension", Numeric("SBP", 90, ExpressionOperator.LT), createdAt),
                Create("Hypotension", Numeric("SBP", 90, ExpressionOperator.LT), createdAt.AddMinutes(-1)),
            ], TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(Query(page: 1, size: 10),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(2, result.Data.Items.Count());
            Assert.Equal(2, result.Data.Metadata.TotalItemCount);
            Assert.Equal(2, result.Data.Items.Select(x => x.Id).Distinct().Count());
        }

        /*=== boundary: no data at all ===*/

        [Fact]
        public async Task GetPagedCriterion_EmptyDatabase_ReturnsEmptySuccess()
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
         * Fail path: GetPagedCriterionHandler has no failure branch. The pagination is
         * already validated by GetPagedCriterionValidator before the handler runs, the
         * projection cannot fail and the operation is read only, so the only failure a
         * database outage would produce is not a behaviour of this feature.
         */
    }
}
