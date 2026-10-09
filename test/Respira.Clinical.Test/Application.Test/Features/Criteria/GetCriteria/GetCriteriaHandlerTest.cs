using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.Criteria.GetCriteria;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.Criteria.GetCriteria
{
    public class GetCriteriaHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly GetCriteriaHandler _handler;
        private readonly IDbContext _context;

        public GetCriteriaHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);

            // Initialize handler
            _handler = new(_context);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Clear leftover data (children first for FK constraints) so the count and
            // ordering assertions are deterministic across runs. IgnoreQueryFilters
            // because soft deleted rows are hidden by the query filter but still
            // occupy the table
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
         * Seeds five live criteria deliberately out of alphabetical order. The formulas
         * mirror the real clinical thresholds (CURB-65 age, fever, hypotension,
         * tachypnea, tachycardia) but the name order and the insertion order are made to
         * differ, so an accidental insertion-order result cannot pass the ordering test.
         *
         * Name ascending : Bacteremia risk, Fever over 38, Hypotension, Sepsis suspicion, Tachycardia
         * Inserted first : Tachycardia, Hypotension, Sepsis suspicion, Bacteremia risk, Fever over 38
         */
        private async Task<Dictionary<string, Guid>> SeedAsync(bool includeSoftDeleted = true)
        {
            var baseTime = DateTimeOffset.UtcNow;
            var criteria = new List<Criterion>
            {
                Create("Tachycardia", TachycardiaFormula(), baseTime),
                Create("Hypotension", HypotensionFormula(), baseTime.AddMinutes(-1)),
                Create("Sepsis suspicion", SepsisFormula(), baseTime.AddMinutes(-2)),
                Create("Bacteremia risk", BacteremiaFormula(), baseTime.AddMinutes(-3)),
                Create("Fever over 38", FeverFormula(), baseTime.AddMinutes(-4)),
            };

            if (includeSoftDeleted)
            {
                // Would sort first alphabetically, so any leak would be obvious
                criteria.Add(Create(
                    "Archived screening",
                    new BooleanConstantFormula(true),
                    baseTime.AddMinutes(-5),
                    isDeleted: true));
            }

            await _context.Criteria.AddRangeAsync(criteria, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            return criteria
                .Where(x => !x.IsDeleted)
                .ToDictionary(x => x.Name, x => x.Id);
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

        private static Formula Numeric(string code, ClinicalValueType valueType, decimal threshold, ExpressionOperator op)
        {
            return new BinaryFormula(
                new VariableFormula(new VariableRef(Guid.CreateVersion7(), code, valueType)),
                new NumericConstantFormula(threshold),
                op);
        }

        // CURB-65: age >= 65 years
        private static Formula BacteremiaFormula() =>
            Numeric("AGE", ClinicalValueType.Numeric, 65, ExpressionOperator.GTE);

        // Fever: body temperature > 38 °C
        private static Formula FeverFormula() =>
            Numeric("TEMP", ClinicalValueType.Numeric, 38, ExpressionOperator.GT);

        // Hypotension: systolic blood pressure < 90 mmHg
        private static Formula HypotensionFormula() =>
            Numeric("SBP", ClinicalValueType.Numeric, 90, ExpressionOperator.LT);

        // Tachypnea: respiratory rate >= 30 breaths/min
        private static Formula SepsisFormula() =>
            Numeric("RR", ClinicalValueType.Numeric, 30, ExpressionOperator.GTE);

        // Tachycardia: heart rate >= 125 bpm (severe sepsis threshold)
        private static Formula TachycardiaFormula() =>
            Numeric("HR", ClinicalValueType.Numeric, 125, ExpressionOperator.GTE);

        #region Happy path

        [Fact]
        public async Task GetCriteria_ReturnsAllSortedByNameAscending_Success()
        {
            var idByName = await SeedAsync();

            var result = await _handler.HandleAsync(new GetCriteriaQuery(),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            // Business rule: the list endpoint feeds pick lists, so it must come back
            // sorted by name ascending regardless of insertion order
            Assert.Equal(
            [
                "Bacteremia risk",
                "Fever over 38",
                "Hypotension",
                "Sepsis suspicion",
                "Tachycardia",
            ], [.. result.Data.Criteria.Select(x => x.Name)]);

            // The projection must keep the id together with the name
            Assert.All(result.Data.Criteria, x => Assert.Equal(idByName[x.Name], x.Id));
            Assert.All(result.Data.Criteria, x => Assert.NotEqual(Guid.Empty, x.Id));
        }

        [Fact]
        public async Task GetCriteria_ExcludesSoftDeleted_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(new GetCriteriaQuery(),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            // Business rule: a soft deleted criterion must never be offered again
            Assert.Equal(5, result.Data.Criteria.Count());
            Assert.DoesNotContain(result.Data.Criteria, x => x.Name == "Archived screening");
        }

        [Fact]
        public async Task GetCriteria_DuplicateNames_AreReturnedSeparately_Success()
        {
            /*
             * Business rule from CreateCriterion: there is no uniqueness check on a
             * criterion name, so two rows may share a name and both must stay visible
             */
            var createdAt = DateTimeOffset.UtcNow;
            await _context.Criteria.AddRangeAsync(
            [
                Create("Sepsis suspicion", SepsisFormula(), createdAt),
                Create("Sepsis suspicion", SepsisFormula(), createdAt.AddMinutes(-1)),
            ], TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(new GetCriteriaQuery(),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(2, result.Data.Criteria.Count());
            Assert.Equal(2, result.Data.Criteria.Count(x => x.Name == "Sepsis suspicion"));
            Assert.Equal(2, result.Data.Criteria.Select(x => x.Id).Distinct().Count());
        }

        [Fact]
        public async Task GetCriteria_SingleCriterion_ReturnsThatCriterion()
        {
            // Lower boundary of the list: exactly one row in the table
            await _context.Criteria.AddAsync(
                Create("Hypotension", HypotensionFormula(), DateTimeOffset.UtcNow),
                TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(new GetCriteriaQuery(),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var item = Assert.Single(result.Data.Criteria);
            Assert.Equal("Hypotension", item.Name);
            Assert.NotEqual(Guid.Empty, item.Id);
        }

        /*=== boundary: no data at all ===*/

        [Fact]
        public async Task GetCriteria_EmptyDatabase_ReturnsEmptySuccess()
        {
            // Lower boundary of the list size: nothing to return, still a success
            var result = await _handler.HandleAsync(new GetCriteriaQuery(),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Empty(result.Data.Criteria);
        }

        #endregion

        /*
         * Fail path: GetCriteriaHandler has no failure branch. The query takes no input,
         * the projection cannot fail and the operation is read only, so the only failure
         * a database outage would produce is not a behaviour of this feature.
         */
    }
}
