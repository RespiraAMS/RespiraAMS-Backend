using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Features.ClinicalMetrics.UpdateMetricRule;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;
using FormulaDto = Respira.Clinical.Application.Features.Shared.ManageFormula.FormulaDto;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Application.Test.Features.ClinicalMetrics.UpdateMetricRule
{
    public class UpdateMetricRuleHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        // The concrete context type is kept so the change tracker can be cleared
        // between tests (the handler still receives it as IDbContext)
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly UpdateMetricRuleHandler _handler;
        private readonly ClinicalDbContext _context;

        private static readonly (string Name, string Code, string Description, string Version, string Publisher) Curb65 =
            ("Severity metrics CURB-65", "CURB-65", "CURB-65 metrics to assess severity of a patient",
                "2002-2003", "Hiệp hội Lồng ngực Anh (BTS)");

        private static readonly (string Name, string Code, string Description, string Version, string Publisher) Psi =
            ("Pneumonia Severity Index (PSI)", "PSI", "PSI metrics to assess severity of a patient with more detail than CURB-65",
                "1997", "Đội ngũ Nghiên cứu Kết quả Điều trị Viêm phổi (PORT - Pneumonia Outcomes Research Team)");

        private static readonly (string Name, string Code, string Description, string Version, string Publisher) IdsaAts =
            ("ICU hospitalization criteria by IDSA/ATS", "IDSA/ATS", "IDSA/ATS metrics to assess whether a severe patient need ICU or not",
                "2026", "Hiệp hội Bệnh nhiễm trùng Mỹ (IDSA) và Hiệp hội Lồng ngực Mỹ (ATS)");

        // Fixed stamp for the rules before the update: a rule that must stay
        // untouched keeps it exactly, a successfully updated rule moves past it
        private static readonly DateTimeOffset OriginalUpdatedAt =
            new(2024, 1, 15, 8, 30, 0, TimeSpan.Zero);

        public UpdateMetricRuleHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var mapper = new UpdateMetricRuleMapper(new FormulaDtoMapper());
            var logger = new Mock<ILogger<UpdateMetricRuleHandler>>().Object;

            // Initialize handler
            _handler = new(_context, mapper, logger);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Clear leftover data (children first for FK constraints) so the count and
            // SingleAsync assertions are deterministic across runs. IgnoreQueryFilters
            // because soft deleted rows are hidden by the query filter but still
            // occupy the table
            await _context.MetricRules.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.ClinicalMetrics.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Criteria.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.ClinicalVariables.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);

            /*
             * The mapper stamps the loaded rule before the formula mapping can fail,
             * so a failed update leaves that rule modified in memory. Dropping those
             * entries keeps the seeding SaveChanges of the next test from flushing
             * stale state against rows the deletes above already removed
             */
            _context.ChangeTracker.Clear();
        }

        #region Happy path

        [Fact]
        public async Task UpdateRule_ReassignCriterion_Success()
        {
            // A rule may be retargeted to another live criterion of the same metric
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var sbpId = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", Numeric("SBP", 90, ExpressionOperator.Lt));
            var ruleId = await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));

            var command = ScoringCommand(ruleId, metricId, sbpId, OnePoint());

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            // Verify through a fresh context so the change tracker of the saving
            // context cannot mask whether the row was truly committed
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = Assert.IsType<ScoringRule>(await freshContext.MetricRules.SingleAsync(
                x => x.Id == ruleId, TestContext.Current.CancellationToken));

            Assert.Equal(metricId, saved.ClinicalMetricId);
            Assert.Equal(sbpId, saved.CriterionId);
            Assert.Equal("1", saved.ScoreFunction.ToString());

            // Business rule: the update stamps the rule so its age reflects the change
            Assert.True(saved.UpdatedAt > OriginalUpdatedAt);
            Assert.False(saved.IsDeleted);
            Assert.Null(saved.DeletedAt);

            // Business rule: an update never adds nor removes rules of the metric
            var savedMetric = await freshContext.ClinicalMetrics
                .Include(x => x.Rules)
                .SingleAsync(x => x.Id == metricId, TestContext.Current.CancellationToken);
            _ = Assert.Single(savedMetric.Rules);
            Assert.True(savedMetric.IsScoringMetric);
            Assert.False(savedMetric.IsMajorMinorMetric);
        }

        [Fact]
        public async Task UpdateRule_NewScoreFunctionWithVariable_Success()
        {
            /*
             * The new score function references a clinical variable, so it only
             * resolves when the handler loads the variables from the database as
             * the mapper dependencies: one point for patients at least 65 years old,
             * nothing below (the age band scoring of the real severity metrics)
             */
            var metricId = await SeedMetricAsync(Psi);
            var bunId = await SeedCriterionAsync(
                "Urea máu >= 30 mg/dL", Numeric("BUN", 30, ExpressionOperator.Gte));
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            // PSI weight: an elevated blood urea nitrogen is worth 10 points
            var ruleId = await SeedScoringRuleAsync(metricId, bunId, new NumericConstantFormula(10));
            var ageVarId = await SeedAgeVariableAsync();

            var command = ScoringCommand(ruleId, metricId, ageId, AgeBandScore(ageVarId));

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            // The nested formula survives the JSONB round trip of the storage
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = Assert.IsType<ScoringRule>(await freshContext.MetricRules.SingleAsync(
                x => x.Id == ruleId, TestContext.Current.CancellationToken));

            Assert.Equal(metricId, saved.ClinicalMetricId);
            Assert.Equal(ageId, saved.CriterionId);
            Assert.True(saved.UpdatedAt > OriginalUpdatedAt);

            var ternary = Assert.IsType<TernaryFormula>(saved.ScoreFunction);
            var condition = Assert.IsType<BinaryFormula>(ternary.Condition);
            Assert.Equal(ExpressionOperator.Gte, condition.Operator);
            Assert.Equal("AGE ≥ 65", condition.ToString());
            Assert.Equal(ageVarId, Assert.IsType<VariableFormula>(condition.Left).Variable.Id);
            Assert.Equal(1m, Assert.IsType<NumericConstantFormula>(ternary.IfTrue).Constant);
            Assert.Equal(0m, Assert.IsType<NumericConstantFormula>(ternary.IfFalse).Constant);
        }

        // Boundary values of the IsMajor flag
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateRule_IsMajorBoundary_Success(bool isMajor)
        {
            // IDSA/ATS style: a criterion is reclassified between minor and major
            var metricId = await SeedMetricAsync(IdsaAts);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var sbpId = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", Numeric("SBP", 90, ExpressionOperator.Lt));
            var ruleId = await SeedMajorMinorRuleAsync(metricId, ageId, isMajor: !isMajor);

            var command = MajorMinorCommand(ruleId, metricId, sbpId, isMajor);

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = Assert.IsType<MajorMinorRule>(await freshContext.MetricRules.SingleAsync(
                x => x.Id == ruleId, TestContext.Current.CancellationToken));

            Assert.Equal(metricId, saved.ClinicalMetricId);
            Assert.Equal(sbpId, saved.CriterionId);

            // Business rule: the flag takes the exact boundary value of the command
            Assert.Equal(isMajor, saved.IsMajor);
            Assert.True(saved.UpdatedAt > OriginalUpdatedAt);
            Assert.False(saved.IsDeleted);

            // Business rule: the metric stays in its major/minor system
            var savedMetric = await freshContext.ClinicalMetrics
                .Include(x => x.Rules)
                .SingleAsync(x => x.Id == metricId, TestContext.Current.CancellationToken);
            _ = Assert.Single(savedMetric.Rules);
            Assert.True(savedMetric.IsMajorMinorMetric);
            Assert.False(savedMetric.IsScoringMetric);
        }

        [Fact]
        public async Task UpdateRule_SiblingRulesStayUntouched_Success()
        {
            // Only the targeted rule may change, its siblings keep their payload
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var sbpId = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", Numeric("SBP", 90, ExpressionOperator.Lt));
            var rrId = await SeedCriterionAsync(
                "Nhịp thở >= 30 lần/phút", Numeric("RR", 30, ExpressionOperator.Gte));
            var ruleId = await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));
            var siblingId = await SeedScoringRuleAsync(metricId, sbpId, new NumericConstantFormula(1));

            var command = ScoringCommand(ruleId, metricId, rrId, OnePoint());

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var rules = await freshContext.MetricRules
                .Where(x => x.ClinicalMetricId == metricId)
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal(2, rules.Count);

            var updated = Assert.IsType<ScoringRule>(rules.Single(x => x.Id == ruleId));
            Assert.Equal(rrId, updated.CriterionId);
            Assert.True(updated.UpdatedAt > OriginalUpdatedAt);

            var sibling = Assert.IsType<ScoringRule>(rules.Single(x => x.Id == siblingId));
            Assert.Equal(sbpId, sibling.CriterionId);
            Assert.Equal("1", sibling.ScoreFunction.ToString());
            Assert.Equal(OriginalUpdatedAt, sibling.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRule_SoftDeletedRuleDoesNotReserveCriterion_Success()
        {
            /*
             * Business rule: the pair uniqueness only covers live rules, so the
             * criterion of a dropped rule is free again for the surviving rules
             */
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var sbpId = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", Numeric("SBP", 90, ExpressionOperator.Lt));
            var ruleId = await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));
            _ = await SeedScoringRuleAsync(metricId, sbpId, new NumericConstantFormula(1), isDeleted: true);

            var command = ScoringCommand(ruleId, metricId, sbpId, OnePoint());

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = Assert.IsType<ScoringRule>(await freshContext.MetricRules.SingleAsync(
                x => x.Id == ruleId, TestContext.Current.CancellationToken));
            Assert.Equal(sbpId, saved.CriterionId);
            Assert.True(saved.UpdatedAt > OriginalUpdatedAt);

            // Only the updated rule counts as live, the soft deleted one still
            // occupies the table
            Assert.Equal(1, await freshContext.MetricRules
                .CountAsync(x => x.ClinicalMetricId == metricId, TestContext.Current.CancellationToken));
            Assert.Equal(2, await freshContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(x => x.ClinicalMetricId == metricId, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task UpdateRule_UpdateScoreFunctionOnly_Success()
        {
            /*
             * The most common update: rewrite only the score function while the rule
             * keeps its own criterion (a guideline reweights the component from 1 to
             * 2 points)
             */
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var ruleId = await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));

            var command = ScoringCommand(ruleId, metricId, ageId, new FormulaDto { Constant = "2" });

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            // Expected: a rule does not duplicate itself, so keeping its criterion
            // must pass the pair uniqueness check
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = Assert.IsType<ScoringRule>(await freshContext.MetricRules.SingleAsync(
                x => x.Id == ruleId, TestContext.Current.CancellationToken));

            Assert.Equal(ageId, saved.CriterionId);
            Assert.Equal("2", saved.ScoreFunction.ToString());
            Assert.True(saved.UpdatedAt > OriginalUpdatedAt);
        }

        [Fact]
        public async Task UpdateRule_ToggleIsMajorOnly_Success()
        {
            // The same natural update for a major/minor rule: only the weight flips
            var metricId = await SeedMetricAsync(IdsaAts);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var ruleId = await SeedMajorMinorRuleAsync(metricId, ageId, isMajor: false);

            var command = MajorMinorCommand(ruleId, metricId, ageId, isMajor: true);

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            // Expected: same source rule - the rule must not count as its own duplicate
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = Assert.IsType<MajorMinorRule>(await freshContext.MetricRules.SingleAsync(
                x => x.Id == ruleId, TestContext.Current.CancellationToken));

            Assert.Equal(ageId, saved.CriterionId);
            Assert.True(saved.IsMajor);
            Assert.True(saved.UpdatedAt > OriginalUpdatedAt);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task UpdateRule_UnknownMetric_Fail()
        {
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var ruleId = await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));

            var command = ScoringCommand(ruleId, Guid.CreateVersion7(), ageId, OnePoint());

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The real rule must stay untouched
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.MetricRules.SingleAsync(
                x => x.Id == ruleId, TestContext.Current.CancellationToken);
            Assert.Equal(ageId, saved.CriterionId);
            Assert.Equal(OriginalUpdatedAt, saved.UpdatedAt);
            Assert.Equal("1", Assert.IsType<ScoringRule>(saved).ScoreFunction.ToString());
        }

        [Fact]
        public async Task UpdateRule_SoftDeletedMetric_Fail()
        {
            // The !IsDeleted query filter hides soft deleted metrics, so the handler
            // reports them exactly like unknown ones
            var metricId = await SeedMetricAsync(Curb65, isDeleted: true);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var ruleId = await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));

            var command = ScoringCommand(ruleId, metricId, ageId, OnePoint());

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.MetricRules.SingleAsync(
                x => x.Id == ruleId, TestContext.Current.CancellationToken);
            Assert.Equal(ageId, saved.CriterionId);
            Assert.Equal(OriginalUpdatedAt, saved.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRule_UnknownRuleId_Fail()
        {
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            _ = await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));

            var command = ScoringCommand(Guid.CreateVersion7(), metricId, ageId, OnePoint());

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.MetricRules.SingleAsync(
                TestContext.Current.CancellationToken);
            Assert.Equal(OriginalUpdatedAt, saved.UpdatedAt);
            Assert.Equal("1", Assert.IsType<ScoringRule>(saved).ScoreFunction.ToString());
        }

        [Fact]
        public async Task UpdateRule_EmptyRuleId_Fail()
        {
            // Boundary: Guid.Empty can never match a stored rule
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            _ = await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));

            var command = ScoringCommand(Guid.Empty, metricId, ageId, OnePoint());

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.MetricRules.SingleAsync(
                TestContext.Current.CancellationToken);
            Assert.Equal(OriginalUpdatedAt, saved.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRule_SoftDeletedRule_Fail()
        {
            // The !IsDeleted query filter hides soft deleted rules inside the metric,
            // so they report like rules that do not exist
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var ruleId = await SeedScoringRuleAsync(
                metricId, ageId, new NumericConstantFormula(1), isDeleted: true);

            var command = ScoringCommand(ruleId, metricId, ageId, OnePoint());

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The dropped rule keeps its payload and its soft delete markers
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.MetricRules.IgnoreQueryFilters().SingleAsync(
                x => x.Id == ruleId, TestContext.Current.CancellationToken);
            Assert.True(saved.IsDeleted);
            Assert.NotNull(saved.DeletedAt);
            Assert.Equal(ageId, saved.CriterionId);
            Assert.Equal(OriginalUpdatedAt, saved.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRule_RuleBelongsToAnotherMetric_Fail()
        {
            /*
             * Business rule: a rule can only be edited through its own metric, so a
             * rule id presented under a foreign metric is reported as missing
             */
            var curbId = await SeedMetricAsync(Curb65);
            var psiId = await SeedMetricAsync(Psi);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var sbpId = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", Numeric("SBP", 90, ExpressionOperator.Lt));
            var rrId = await SeedCriterionAsync(
                "Nhịp thở >= 30 lần/phút", Numeric("RR", 30, ExpressionOperator.Gte));
            _ = await SeedScoringRuleAsync(curbId, ageId, new NumericConstantFormula(1));
            var foreignRuleId = await SeedScoringRuleAsync(psiId, sbpId, new NumericConstantFormula(1));

            var command = ScoringCommand(foreignRuleId, curbId, rrId, OnePoint());

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The foreign rule must stay untouched
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.MetricRules.SingleAsync(
                x => x.Id == foreignRuleId, TestContext.Current.CancellationToken);
            Assert.Equal(psiId, saved.ClinicalMetricId);
            Assert.Equal(sbpId, saved.CriterionId);
            Assert.Equal(OriginalUpdatedAt, saved.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRule_UnknownCriterion_Fail()
        {
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var ruleId = await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));

            var command = ScoringCommand(ruleId, metricId, Guid.CreateVersion7(), OnePoint());

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.MetricRules.SingleAsync(
                x => x.Id == ruleId, TestContext.Current.CancellationToken);
            Assert.Equal(ageId, saved.CriterionId);
            Assert.Equal(OriginalUpdatedAt, saved.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRule_SoftDeletedCriterion_Fail()
        {
            // Soft deleted criteria are hidden from the existence check the same way
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var ruleId = await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));
            var deletedCriterionId = await SeedCriterionAsync(
                "Nhịp thở >= 30 lần/phút", Numeric("RR", 30, ExpressionOperator.Gte), isDeleted: true);

            var command = ScoringCommand(ruleId, metricId, deletedCriterionId, OnePoint());

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.MetricRules.SingleAsync(
                x => x.Id == ruleId, TestContext.Current.CancellationToken);
            Assert.Equal(ageId, saved.CriterionId);
            Assert.Equal(OriginalUpdatedAt, saved.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRule_DuplicateCriterion_Fail()
        {
            /*
             * Business rule: two live rules of the same metric never share a
             * criterion. Boundary: exactly one other live rule already holds the
             * target criterion
             */
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var sbpId = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", Numeric("SBP", 90, ExpressionOperator.Lt));
            var ruleId = await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));
            var occupiedId = await SeedScoringRuleAsync(metricId, sbpId, new NumericConstantFormula(1));

            var command = ScoringCommand(ruleId, metricId, sbpId, OnePoint());

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Both rules must keep their original payload
            await using var freshContext = new ClinicalDbContext(_options);
            var rules = await freshContext.MetricRules.IgnoreQueryFilters()
                .Where(x => x.ClinicalMetricId == metricId)
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal(2, rules.Count);

            var target = rules.Single(x => x.Id == ruleId);
            Assert.Equal(ageId, target.CriterionId);
            Assert.Equal(OriginalUpdatedAt, target.UpdatedAt);

            var occupied = rules.Single(x => x.Id == occupiedId);
            Assert.Equal(sbpId, occupied.CriterionId);
            Assert.Equal(OriginalUpdatedAt, occupied.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRule_ScoringRuleToMajorMinor_Fail()
        {
            /*
             * Business rule: the TPH type of a stored rule cannot change, so a
             * scoring rule rejects a major/minor command. The free criterion keeps
             * the pair uniqueness out of the way
             */
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var rrId = await SeedCriterionAsync(
                "Nhịp thở >= 30 lần/phút", Numeric("RR", 30, ExpressionOperator.Gte));
            var ruleId = await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));

            var command = MajorMinorCommand(ruleId, metricId, rrId, isMajor: true);

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.MetricRules.SingleAsync(
                x => x.Id == ruleId, TestContext.Current.CancellationToken);
            var scoring = Assert.IsType<ScoringRule>(saved);
            Assert.Equal(ageId, scoring.CriterionId);
            Assert.Equal("1", scoring.ScoreFunction.ToString());
            Assert.Equal(OriginalUpdatedAt, scoring.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRule_MajorMinorRuleToScoring_Fail()
        {
            // The other direction of the same rule: a major/minor rule rejects a
            // score function
            var metricId = await SeedMetricAsync(IdsaAts);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var rrId = await SeedCriterionAsync(
                "Nhịp thở >= 30 lần/phút", Numeric("RR", 30, ExpressionOperator.Gte));
            var ruleId = await SeedMajorMinorRuleAsync(metricId, ageId, isMajor: false);

            var command = ScoringCommand(ruleId, metricId, rrId, OnePoint());

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.MetricRules.SingleAsync(
                x => x.Id == ruleId, TestContext.Current.CancellationToken);
            var majorMinor = Assert.IsType<MajorMinorRule>(saved);
            Assert.Equal(ageId, majorMinor.CriterionId);
            Assert.False(majorMinor.IsMajor);
            Assert.Equal(OriginalUpdatedAt, majorMinor.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRule_WithoutScoreFunctionNorIsMajor_Fail()
        {
            /*
             * Boundary: both nullable fields at their null boundary. Even when the
             * mutual exclusion of the validator is bypassed, a scoring rule still
             * needs its score function, so the handler rejects the command cleanly
             * instead of reaching the mapper
             */
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var rrId = await SeedCriterionAsync(
                "Nhịp thở >= 30 lần/phút", Numeric("RR", 30, ExpressionOperator.Gte));
            var ruleId = await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));

            var command = new UpdateMetricRuleCommand
            {
                Id = ruleId,
                ClinicalMetricId = metricId,
                CriterionId = rrId,
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = Assert.IsType<ScoringRule>(await freshContext.MetricRules.SingleAsync(
                x => x.Id == ruleId, TestContext.Current.CancellationToken));
            Assert.Equal(ageId, saved.CriterionId);
            Assert.Equal("1", saved.ScoreFunction.ToString());
            Assert.Equal(OriginalUpdatedAt, saved.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRule_UnknownVariableInScoreFunction_Fail()
        {
            /*
             * The formula shape passes validation, but the referenced clinical
             * variable does not exist, so the formula mapping fails and the handler
             * forwards the failure without saving
             */
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var rrId = await SeedCriterionAsync(
                "Nhịp thở >= 30 lần/phút", Numeric("RR", 30, ExpressionOperator.Gte));
            var ruleId = await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));

            var command = ScoringCommand(ruleId, metricId, rrId, new FormulaDto { Variable = Guid.CreateVersion7() });

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The mapper stamps the rule in memory before the formula fails, but
            // without a save the database must still hold the original payload
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = Assert.IsType<ScoringRule>(await freshContext.MetricRules.SingleAsync(
                x => x.Id == ruleId, TestContext.Current.CancellationToken));
            Assert.Equal(ageId, saved.CriterionId);
            Assert.Equal("1", saved.ScoreFunction.ToString());
            Assert.Equal(OriginalUpdatedAt, saved.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRule_SoftDeletedVariableInScoreFunction_Fail()
        {
            // Soft deleted variables are hidden from the dependencies the handler
            // loads, so they cannot be referenced by an updated rule either
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var rrId = await SeedCriterionAsync(
                "Nhịp thở >= 30 lần/phút", Numeric("RR", 30, ExpressionOperator.Gte));
            var ruleId = await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));
            var ageVarId = await SeedAgeVariableAsync(isDeleted: true);

            var command = ScoringCommand(ruleId, metricId, rrId, new FormulaDto { Variable = ageVarId });

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = Assert.IsType<ScoringRule>(await freshContext.MetricRules.SingleAsync(
                x => x.Id == ruleId, TestContext.Current.CancellationToken));
            Assert.Equal(ageId, saved.CriterionId);
            Assert.Equal(OriginalUpdatedAt, saved.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRule_TypeInvalidFormulaOperands_Fail()
        {
            /*
             * The formula is shape-complete (left, right and operator all present),
             * so it passes validation, but adding a boolean to a number has no
             * meaning in the domain - the mapper reports the failure the domain
             * model raised and nothing reaches the database
             */
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var rrId = await SeedCriterionAsync(
                "Nhịp thở >= 30 lần/phút", Numeric("RR", 30, ExpressionOperator.Gte));
            var ruleId = await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));

            var command = ScoringCommand(ruleId, metricId, rrId, new FormulaDto
            {
                Operator = ExpressionOperator.Add,
                Left = new FormulaDto { Constant = "true" },
                Right = new FormulaDto { Constant = "1" },
            });

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = Assert.IsType<ScoringRule>(await freshContext.MetricRules.SingleAsync(
                x => x.Id == ruleId, TestContext.Current.CancellationToken));
            Assert.Equal(ageId, saved.CriterionId);
            Assert.Equal("1", saved.ScoreFunction.ToString());
            Assert.Equal(OriginalUpdatedAt, saved.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRule_BothScoreFunctionAndIsMajor_Throws()
        {
            /*
             * The FluentValidation middleware rejects such a command before the
             * handler in production; the handler forwards the command straight to
             * the mapper, which throws on the contradiction, so this is pinned here
             * as well
             */
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var rrId = await SeedCriterionAsync(
                "Nhịp thở >= 30 lần/phút", Numeric("RR", 30, ExpressionOperator.Gte));
            var ruleId = await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));

            var command = ScoringCommand(ruleId, metricId, rrId, OnePoint());
            command.IsMajor = true;

            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.HandleAsync(command, TestContext.Current.CancellationToken));

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.MetricRules.SingleAsync(
                x => x.Id == ruleId, TestContext.Current.CancellationToken);
            Assert.Equal(ageId, saved.CriterionId);
            Assert.Equal(OriginalUpdatedAt, saved.UpdatedAt);
        }

        #endregion

        private async Task<Guid> SeedMetricAsync(
            (string Name, string Code, string Description, string Version, string Publisher) metric,
            bool isDeleted = false)
        {
            var entity = new ClinicalMetric(
                metric.Name, metric.Code, metric.Description, metric.Version, metric.Publisher, isApplied: true, [])
            {
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.ClinicalMetrics.AddAsync(entity, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return entity.Id;
        }

        private async Task<Guid> SeedCriterionAsync(string name, Formula formula, bool isDeleted = false)
        {
            var criterion = new Criterion(name, formula)
            {
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.Criteria.AddAsync(criterion, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return criterion.Id;
        }

        private async Task<Guid> SeedAgeVariableAsync(bool isDeleted = false)
        {
            var age = new NumericClinicalVariable
            {
                // Patient age, the CURB-65 severity threshold sits at 65 years
                Code = "AGE",
                Name = "Tuổi",
                Description = "Patient age in years",
                CanonicalUnit = "year",
                IsRequired = true,
                Category = ClinicalVariableCategory.Clinical,
                AcceptedRange = CreateRange(0, 120, "year"),
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.ClinicalVariables.AddAsync(age, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return age.Id;
        }

        /*
         * Rules are seeded through a fresh context: the handler must see only what
         * its own query returns, exactly like in production. Seeding through the
         * handler context would hand soft deleted rules to the handler via EF
         * relationship fixup even though the query filter hides them. Every seed
         * also carries the fixed pre-update stamp so untouched rules can be told
         * apart from updated ones
         */
        private async Task<Guid> SeedScoringRuleAsync(
            Guid metricId, Guid criterionId, Formula scoreFunction, bool isDeleted = false)
        {
            await using var seedContext = new ClinicalDbContext(_options);
            var rule = new ScoringRule
            {
                ClinicalMetricId = metricId,
                CriterionId = criterionId,
                ScoreFunction = scoreFunction,
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
                UpdatedAt = OriginalUpdatedAt,
            };
            await seedContext.MetricRules.AddAsync(rule, TestContext.Current.CancellationToken);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            return rule.Id;
        }

        private async Task<Guid> SeedMajorMinorRuleAsync(
            Guid metricId, Guid criterionId, bool isMajor, bool isDeleted = false)
        {
            await using var seedContext = new ClinicalDbContext(_options);
            var rule = new MajorMinorRule
            {
                ClinicalMetricId = metricId,
                CriterionId = criterionId,
                IsMajor = isMajor,
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
                UpdatedAt = OriginalUpdatedAt,
            };
            await seedContext.MetricRules.AddAsync(rule, TestContext.Current.CancellationToken);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            return rule.Id;
        }

        private static UpdateMetricRuleCommand ScoringCommand(
            Guid ruleId, Guid metricId, Guid criterionId, FormulaDto scoreFunction) => new()
            {
                Id = ruleId,
                ClinicalMetricId = metricId,
                CriterionId = criterionId,
                ScoreFunction = scoreFunction,
            };

        private static UpdateMetricRuleCommand MajorMinorCommand(
            Guid ruleId, Guid metricId, Guid criterionId, bool isMajor) => new()
            {
                Id = ruleId,
                ClinicalMetricId = metricId,
                CriterionId = criterionId,
                IsMajor = isMajor,
            };

        private static FormulaDto OnePoint() => new() { Constant = "1" };

        // One point for patients at least 65 years old, nothing below
        private static FormulaDto AgeBandScore(Guid ageVariableId) => new()
        {
            Condition = new FormulaDto
            {
                Operator = ExpressionOperator.Gte,
                Left = new FormulaDto { Variable = ageVariableId },
                Right = new FormulaDto { Constant = "65" },
            },
            IfTrue = new FormulaDto { Constant = "1" },
            IfFalse = new FormulaDto { Constant = "0" },
        };

        private static Formula Numeric(string code, decimal threshold, ExpressionOperator op) =>
            new BinaryFormula(
                Variable(code), new NumericConstantFormula(threshold), op);

        private static Formula Variable(string code) => new VariableFormula(
            new VariableRef(Guid.CreateVersion7(), code, ClinicalValueType.Numeric));

        private static Range CreateRange(decimal min, decimal max, string unit) => new()
        {
            Min = min,
            IsMinExclusive = false,
            Max = max,
            IsMaxExclusive = false,
            Unit = unit,
        };
    }
}
