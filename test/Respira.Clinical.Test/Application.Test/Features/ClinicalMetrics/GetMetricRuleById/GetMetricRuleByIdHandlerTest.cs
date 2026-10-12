using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.ClinicalMetrics.GetMetricRuleById;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.ClinicalMetrics.GetMetricRuleById
{
    public class GetMetricRuleByIdHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private static readonly (string Name, string Code, string Description, string Version, string Publisher) Curb65 =
            ("Severity metrics CURB-65", "CURB-65", "CURB-65 metrics to assess severity of a patient",
                "2002-2003", "Hiệp hội Lồng ngực Anh (BTS)");

        private static readonly (string Name, string Code, string Description, string Version, string Publisher) Psi =
            ("Pneumonia Severity Index (PSI)", "PSI", "PSI metrics to assess severity of a patient with more detail than CURB-65",
                "1997", "Đội ngũ Nghiên cứu Kết quả Điều trị Viêm phổi (PORT - Pneumonia Outcomes Research Team)");

        private static readonly (string Name, string Code, string Description, string Version, string Publisher) IdsaAts =
            ("ICU hospitalization criteria by IDSA/ATS", "IDSA/ATS", "IDSA/ATS metrics to assess whether a severe patient need ICU or not",
                "2026", "Hiệp hội Bệnh nhiễm trùng Mỹ (IDSA) và Hiệp hội Lồng ngực Mỹ (ATS)");

        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly GetMetricRuleByIdHandler _handler;
        private readonly IDbContext _context;

        public GetMetricRuleByIdHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>()
                .UseNpgsql(fixture.ConnectionString).Options;
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
            // Clear leftover data (children first for FK constraints) so the not found
            // assertions are deterministic across runs. IgnoreQueryFilters because soft
            // deleted rows are hidden by the query filter but still occupy the table
            await _context.MetricRules.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.ClinicalMetrics.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Criteria.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        #region Happy path

        [Fact]
        public async Task GetMetricRuleById_ScoringRule_Success()
        {
            var metricId = await SeedMetricAsync(Curb65);
            var age = await SeedCriterionAsync("Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.GTE));

            // Every CURB-65 component is worth exactly one point, like in the seed data
            var ruleId = await SeedScoringRuleAsync(metricId, age, new NumericConstantFormula(1));

            var result = await _handler.HandleAsync(
                new GetMetricRuleByIdQuery { Id = ruleId }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            // Business rule: the read exposes the rule together with its parent metric
            // and its criterion, so the scoring system can be understood from the read
            Assert.Equal(ruleId, result.Data.Id);
            Assert.Equal(metricId, result.Data.ClinicalMetricId);
            Assert.Equal(Curb65.Name, result.Data.ClinicalMetricName);
            Assert.Equal(age, result.Data.Criterion.Id);
            Assert.Equal("Tuổi >= 65", result.Data.Criterion.Name);
            Assert.Equal("AGE ≥ 65", result.Data.Criterion.Formula);

            // Business rule: a scoring rule renders its score function and carries no
            // major/minor flag
            Assert.Equal("1", result.Data.ScoreFunction);
            Assert.Null(result.Data.IsMajor);
        }

        [Fact]
        public async Task GetMetricRuleById_ScoreFunctionVariants_Success()
        {
            /*
             * Business rule: the score function is rendered back from its stored JSONB
             * payload. The real PSI seed mixes constant, variable and arithmetic score
             * functions, plus one point for the age band of the PSI weight
             */
            var metricId = await SeedMetricAsync(Psi);
            var age = await SeedCriterionAsync("Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.GTE));

            var ruleIds = new[]
            {
                await SeedScoringRuleAsync(metricId, age, new NumericConstantFormula(30)),
                await SeedScoringRuleAsync(metricId, age, Variable("AGE")),
                await SeedScoringRuleAsync(metricId, age, new BinaryFormula(
                    Variable("AGE"), new NumericConstantFormula(10), ExpressionOperator.SUB)),
                await SeedScoringRuleAsync(metricId, age, new TernaryFormula(
                    new BinaryFormula(Variable("AGE"), new NumericConstantFormula(65), ExpressionOperator.GTE),
                    new NumericConstantFormula(1),
                    new NumericConstantFormula(0))),
            };

            // Constant, variable, arithmetic and age band score functions; the arithmetic
            // one renders with the mathematical minus sign of the SUB operator
            string?[] expectedScoreFunctions = ["30", "AGE", "AGE − 10", "If (AGE ≥ 65) then 1 else 0"];
            for (var i = 0; i < ruleIds.Length; i++)
            {
                var result = await _handler.HandleAsync(
                    new GetMetricRuleByIdQuery { Id = ruleIds[i] }, TestContext.Current.CancellationToken);

                Assert.True(result.IsSuccess());
                Assert.NotNull(result.Data);
                Assert.Equal(ruleIds[i], result.Data.Id);
                Assert.Equal(expectedScoreFunctions[i], result.Data.ScoreFunction);
                Assert.Null(result.Data.IsMajor);
            }
        }

        public static readonly TheoryData<bool> MajorFlags =
        [
            // Boundary values of the major/minor flag
            true,
            false,
        ];

        [Theory]
        [MemberData(nameof(MajorFlags))]
        public async Task GetMetricRuleById_MajorMinorRule_Success(bool isMajor)
        {
            // The IDSA/ATS metric counts major and minor criteria instead of scoring points
            var metricId = await SeedMetricAsync(IdsaAts);
            var confusion = await SeedCriterionAsync("Thay đổi tri giác", Confusion());

            var ruleId = await SeedMajorMinorRuleAsync(metricId, confusion, isMajor);

            var result = await _handler.HandleAsync(
                new GetMetricRuleByIdQuery { Id = ruleId }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            Assert.Equal(ruleId, result.Data.Id);
            Assert.Equal(IdsaAts.Name, result.Data.ClinicalMetricName);
            Assert.Equal(confusion, result.Data.Criterion.Id);
            Assert.Equal("Thay đổi tri giác", result.Data.Criterion.Name);
            Assert.Equal("CONFUSION", result.Data.Criterion.Formula);

            // Business rule: a major/minor rule carries no score function, only its weight
            Assert.Equal(isMajor, result.Data.IsMajor);
            Assert.Null(result.Data.ScoreFunction);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task GetMetricRuleById_UnknownId_Fail()
        {
            var result = await _handler.HandleAsync(
                new GetMetricRuleByIdQuery { Id = Guid.CreateVersion7() },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetMetricRuleById_EmptyId_Fail()
        {
            // Boundary: Guid.Empty can never match a stored rule
            var result = await _handler.HandleAsync(
                new GetMetricRuleByIdQuery { Id = Guid.Empty },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetMetricRuleById_SoftDeletedRule_Fail()
        {
            // The !IsDeleted query filter hides soft deleted rows, so reading one is
            // reported exactly like an unknown id
            var metricId = await SeedMetricAsync(Curb65);
            var age = await SeedCriterionAsync("Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.GTE));
            var ruleId = await SeedScoringRuleAsync(
                metricId, age, new NumericConstantFormula(1), isDeleted: true);

            var result = await _handler.HandleAsync(
                new GetMetricRuleByIdQuery { Id = ruleId },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
            Assert.Null(result.Data);
        }

        /*
         * Skipped: a live rule whose ClinicalMetric or Criterion navigation is soft
         * deleted would null deref in the projection (rule.ClinicalMetric.Name), but
         * that state cannot arise in reality - DeleteClinicalMetricHandler cascades the
         * soft delete to every rule of the metric, DeleteCriterionHandler cascades to
         * every rule of the criterion, and AddRule/UpdateRule only accept live parents,
         * so a rule that is still live always has live navigations
         */

        #endregion

        private async Task<Guid> SeedMetricAsync(
            (string Name, string Code, string Description, string Version, string Publisher) metric,
            bool isApplied = true)
        {
            var entity = new ClinicalMetric(
                metric.Name,
                metric.Code,
                metric.Description,
                metric.Version,
                metric.Publisher,
                isApplied,
                []);
            await _context.ClinicalMetrics.AddAsync(entity, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return entity.Id;
        }

        // The formula mirrors the real seed data so the seeded criterion stays a legal
        // boolean criterion (CURB-65 age >= 65, hypotension SBP < 90 mmHg)
        private async Task<Guid> SeedCriterionAsync(string name, Formula formula)
        {
            var criterion = new Criterion(name, formula);
            await _context.Criteria.AddAsync(criterion, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return criterion.Id;
        }

        private async Task<Guid> SeedScoringRuleAsync(
            Guid metricId, Guid criterionId, Formula scoreFunction, bool isDeleted = false)
        {
            var rule = new ScoringRule
            {
                ClinicalMetricId = metricId,
                CriterionId = criterionId,
                ScoreFunction = scoreFunction,
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.MetricRules.AddAsync(rule, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return rule.Id;
        }

        private async Task<Guid> SeedMajorMinorRuleAsync(Guid metricId, Guid criterionId, bool isMajor)
        {
            var rule = new MajorMinorRule
            {
                ClinicalMetricId = metricId,
                CriterionId = criterionId,
                IsMajor = isMajor,
            };
            await _context.MetricRules.AddAsync(rule, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return rule.Id;
        }

        private static Formula Numeric(string code, decimal threshold, ExpressionOperator op) =>
            new BinaryFormula(
                Variable(code), new NumericConstantFormula(threshold), op);

        // A bare variable reference, like the PSI score functions that add or subtract
        // from the age of the patient
        private static Formula Variable(string code) => new VariableFormula(
            new VariableRef(Guid.CreateVersion7(), code, ClinicalValueType.Numeric));

        private static Formula Confusion() => new VariableFormula(
            new VariableRef(Guid.CreateVersion7(), "CONFUSION", ClinicalValueType.Boolean));
    }
}
