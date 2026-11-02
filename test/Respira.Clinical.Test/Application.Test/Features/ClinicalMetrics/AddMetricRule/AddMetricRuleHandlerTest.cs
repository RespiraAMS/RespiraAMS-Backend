using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.ClinicalMetrics.AddMetricRule;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;
using FormulaDto = Respira.Clinical.Application.Features.Shared.ManageFormula.FormulaDto;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Application.Test.Features.ClinicalMetrics.AddMetricRule
{
    public class AddMetricRuleHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
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
        private readonly AddMetricRuleHandler _handler;
        private readonly IDbContext _context;

        public AddMetricRuleHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var mapper = new AddMetricRuleMapper(new FormulaDtoMapper());
            var logger = new Mock<ILogger<AddMetricRuleHandler>>().Object;

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
        }

        #region Happy path

        [Fact]
        public async Task AddRule_ScoringRuleConstant_Success()
        {
            /*
             * CURB-65 style: each of the five components is worth exactly one point,
             * so the score function is the constant 1
             */
            var metricId = await SeedMetricAsync(Curb65);
            var criterionId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));

            var result = await _handler.HandleAsync(
                ScoringAdd(metricId, criterionId, OnePoint()),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.NotEqual(Guid.Empty, result.Data.Id);

            // Verify through a fresh context so the change tracker of the saving
            // context cannot mask whether the row was truly committed
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = Assert.IsType<ScoringRule>(await freshContext.MetricRules.SingleAsync(
                x => x.Id == result.Data.Id, TestContext.Current.CancellationToken));

            Assert.Equal(metricId, saved.ClinicalMetricId);
            Assert.Equal(criterionId, saved.CriterionId);
            Assert.Equal("1", saved.ScoreFunction.ToString());
            Assert.False(saved.IsDeleted);
            Assert.Null(saved.DeletedAt);

            // Business rule: the first scoring rule turns the metric into a scoring
            // system
            var savedMetric = await freshContext.ClinicalMetrics
                .Include(x => x.Rules)
                .SingleAsync(x => x.Id == metricId, TestContext.Current.CancellationToken);
            _ = Assert.Single(savedMetric.Rules);
            Assert.True(savedMetric.IsScoringMetric);
            Assert.False(savedMetric.IsMajorMinorMetric);
        }

        [Fact]
        public async Task AddRule_ScoringRuleTernaryWithVariable_Success()
        {
            /*
             * A score function that references a clinical variable only resolves when
             * the handler loads the variables from the database as the mapper
             * dependencies: one point for patients at least 65 years old, nothing
             * below (the age band scoring of the real severity metrics)
             */
            var metricId = await SeedMetricAsync(Curb65);
            var criterionId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var ageId = await SeedAgeVariableAsync();

            var result = await _handler.HandleAsync(
                ScoringAdd(metricId, criterionId, AgeBandScore(ageId)),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);

            // The nested formula survives the JSONB round trip of the storage
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = Assert.IsType<ScoringRule>(await freshContext.MetricRules.SingleAsync(
                x => x.Id == result.Data.Id, TestContext.Current.CancellationToken));

            Assert.Equal(metricId, saved.ClinicalMetricId);
            Assert.Equal(criterionId, saved.CriterionId);

            var ternary = Assert.IsType<TernaryFormula>(saved.ScoreFunction);
            var condition = Assert.IsType<BinaryFormula>(ternary.Condition);
            Assert.Equal(ExpressionOperator.Gte, condition.Operator);
            Assert.Equal("AGE ≥ 65", condition.ToString());
            Assert.Equal(ageId, Assert.IsType<VariableFormula>(condition.Left).Variable.Id);
            Assert.Equal(1m, Assert.IsType<NumericConstantFormula>(ternary.IfTrue).Constant);
            Assert.Equal(0m, Assert.IsType<NumericConstantFormula>(ternary.IfFalse).Constant);
        }

        // Boundary values of the IsMajor flag
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task AddRule_MajorMinorRule_Success(bool isMajor)
        {
            // IDSA/ATS style: the rule only carries its weight, no score function
            var metricId = await SeedMetricAsync(IdsaAts);
            var criterionId = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", Numeric("SBP", 90, ExpressionOperator.Lt));

            var result = await _handler.HandleAsync(
                MajorMinorAdd(metricId, criterionId, isMajor),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = Assert.IsType<MajorMinorRule>(await freshContext.MetricRules.SingleAsync(
                x => x.Id == result.Data.Id, TestContext.Current.CancellationToken));

            Assert.Equal(metricId, saved.ClinicalMetricId);
            Assert.Equal(criterionId, saved.CriterionId);
            Assert.Equal(isMajor, saved.IsMajor);
            Assert.False(saved.IsDeleted);
            Assert.Null(saved.DeletedAt);

            // Business rule: the first major/minor rule turns the metric into a
            // major/minor system
            var savedMetric = await freshContext.ClinicalMetrics
                .Include(x => x.Rules)
                .SingleAsync(x => x.Id == metricId, TestContext.Current.CancellationToken);
            _ = Assert.Single(savedMetric.Rules);
            Assert.True(savedMetric.IsMajorMinorMetric);
            Assert.False(savedMetric.IsScoringMetric);
        }

        [Fact]
        public async Task AddRule_SecondScoringRuleSameSystem_Success()
        {
            // A second scoring rule keeps the metric a pure scoring system
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));
            var sbpId = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", Numeric("SBP", 90, ExpressionOperator.Lt));

            var result = await _handler.HandleAsync(
                ScoringAdd(metricId, sbpId, OnePoint()),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            var rules = await freshContext.MetricRules
                .Where(x => x.ClinicalMetricId == metricId)
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal(2, rules.Count);
            Assert.All(rules, x => Assert.IsType<ScoringRule>(x));
            Assert.Contains(rules, x => x.Id == result.Data.Id);

            var savedMetric = await freshContext.ClinicalMetrics
                .Include(x => x.Rules)
                .SingleAsync(x => x.Id == metricId, TestContext.Current.CancellationToken);
            Assert.True(savedMetric.IsScoringMetric);
            Assert.False(savedMetric.IsMajorMinorMetric);
        }

        [Fact]
        public async Task AddRule_SameCriterionInAnotherMetric_Success()
        {
            /*
             * Business rule: the uniqueness is scoped to one metric, the same
             * criterion may score differently in two metrics
             */
            var curbId = await SeedMetricAsync(Curb65);
            var psiId = await SeedMetricAsync(Psi);
            var criterionId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            await SeedScoringRuleAsync(curbId, criterionId, new NumericConstantFormula(1));

            var result = await _handler.HandleAsync(
                ScoringAdd(psiId, criterionId, OnePoint()),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            var curbRules = await freshContext.MetricRules
                .Where(x => x.ClinicalMetricId == curbId)
                .ToListAsync(TestContext.Current.CancellationToken);
            var psiRules = await freshContext.MetricRules
                .Where(x => x.ClinicalMetricId == psiId)
                .ToListAsync(TestContext.Current.CancellationToken);
            _ = Assert.Single(curbRules);
            var added = Assert.Single(psiRules);
            Assert.Equal(result.Data.Id, added.Id);
        }

        [Fact]
        public async Task AddRule_SoftDeletedRuleDoesNotBlock_Success()
        {
            /*
             * Business rule: the pair uniqueness only covers live rules, a dropped
             * rule no longer reserves its criterion inside the metric
             */
            var metricId = await SeedMetricAsync(Curb65);
            var criterionId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            await SeedScoringRuleAsync(
                metricId, criterionId, new NumericConstantFormula(1), isDeleted: true);

            var result = await _handler.HandleAsync(
                ScoringAdd(metricId, criterionId, OnePoint()),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            // Only the new rule counts as live, the soft deleted one still occupies
            // the table
            Assert.Equal(1, await freshContext.MetricRules
                .CountAsync(x => x.ClinicalMetricId == metricId, TestContext.Current.CancellationToken));
            Assert.Equal(2, await freshContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(x => x.ClinicalMetricId == metricId, TestContext.Current.CancellationToken));
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task AddRule_UnknownMetric_Fail()
        {
            var criterionId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));

            var result = await _handler.HandleAsync(
                ScoringAdd(Guid.CreateVersion7(), criterionId, OnePoint()),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AddRule_SoftDeletedMetric_Fail()
        {
            // The !IsDeleted query filter hides soft deleted metrics, so the handler
            // reports them exactly like unknown ones
            var metricId = await SeedMetricAsync(Curb65, isDeleted: true);
            var criterionId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));

            var result = await _handler.HandleAsync(
                ScoringAdd(metricId, criterionId, OnePoint()),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AddRule_UnknownCriterion_Fail()
        {
            var metricId = await SeedMetricAsync(Curb65);

            var result = await _handler.HandleAsync(
                ScoringAdd(metricId, Guid.CreateVersion7(), OnePoint()),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AddRule_SoftDeletedCriterion_Fail()
        {
            // The !IsDeleted query filter hides soft deleted criteria the same way
            var metricId = await SeedMetricAsync(Curb65);
            var criterionId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte), isDeleted: true);

            var result = await _handler.HandleAsync(
                ScoringAdd(metricId, criterionId, OnePoint()),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AddRule_DuplicateCriterion_Fail()
        {
            /*
             * Boundary: exactly one live rule already holds this pair, the smallest
             * count that triggers the uniqueness rule. The duplicate must not be
             * persisted
             */
            var metricId = await SeedMetricAsync(Curb65);
            var criterionId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            await SeedScoringRuleAsync(metricId, criterionId, new NumericConstantFormula(1));

            var result = await _handler.HandleAsync(
                ScoringAdd(metricId, criterionId, OnePoint()),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(1, await freshContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(x => x.ClinicalMetricId == metricId, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AddRule_UnknownVariableInScoreFunction_Fail()
        {
            /*
             * The formula shape passes validation, but the referenced clinical
             * variable does not exist, so the formula mapping fails and the handler
             * forwards the failure without touching the database
             */
            var metricId = await SeedMetricAsync(Curb65);
            var criterionId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));

            var result = await _handler.HandleAsync(
                ScoringAdd(metricId, criterionId, new FormulaDto { Variable = Guid.CreateVersion7() }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AddRule_SoftDeletedVariableInScoreFunction_Fail()
        {
            // Soft deleted variables are hidden from the dependencies the handler
            // loads, so they cannot be referenced by a new rule either
            var metricId = await SeedMetricAsync(Curb65);
            var criterionId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            var ageId = await SeedAgeVariableAsync(isDeleted: true);

            var result = await _handler.HandleAsync(
                ScoringAdd(metricId, criterionId, new FormulaDto { Variable = ageId }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AddRule_TypeInvalidFormulaOperands_Fail()
        {
            /*
             * The formula is shape-complete (left, right and operator all present),
             * so it passes validation, but adding a boolean to a number has no
             * meaning in the domain - the mapper reports the failure the domain
             * model raised and nothing reaches the database
             */
            var metricId = await SeedMetricAsync(Curb65);
            var criterionId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));

            var result = await _handler.HandleAsync(
                ScoringAdd(metricId, criterionId, new FormulaDto
                {
                    Operator = ExpressionOperator.Add,
                    Left = new FormulaDto { Constant = "true" },
                    Right = new FormulaDto { Constant = "1" },
                }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AddRule_BothScoreFunctionAndIsMajor_Throws()
        {
            /*
             * The FluentValidation middleware rejects such a command before the
             * handler in production; the handler forwards the command straight to
             * the mapper, which throws on the contradiction, so this is pinned here
             * as well
             */
            var metricId = await SeedMetricAsync(Curb65);
            var criterionId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));

            var command = ScoringAdd(metricId, criterionId, OnePoint());
            command.IsMajor = true;

            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.HandleAsync(command, TestContext.Current.CancellationToken));

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AddRule_NeitherScoreFunctionNorIsMajor_Throws()
        {
            // Boundary: both nullable fields at their null boundary at the same time
            var metricId = await SeedMetricAsync(Curb65);
            var criterionId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));

            var command = new AddMetricRuleCommand
            {
                ClinicalMetricId = metricId,
                CriterionId = criterionId,
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.HandleAsync(command, TestContext.Current.CancellationToken));

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AddRule_MajorMinorRuleIntoScoringMetric_Fail()
        {
            // The metric is a scoring system, a major/minor rule must be rejected
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));
            await SeedScoringRuleAsync(metricId, ageId, new NumericConstantFormula(1));
            var sbpId = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", Numeric("SBP", 90, ExpressionOperator.Lt));

            var result = await _handler.HandleAsync(
                MajorMinorAdd(metricId, sbpId, isMajor: false),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);
            Assert.Null(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(1, await freshContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(x => x.ClinicalMetricId == metricId, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AddRule_ScoringRuleIntoMajorMinorMetric_Fail()
        {
            // The metric is a major/minor system, a scoring rule must be rejected
            var metricId = await SeedMetricAsync(IdsaAts);
            var sbpId = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", Numeric("SBP", 90, ExpressionOperator.Lt));
            await SeedMajorMinorRuleAsync(metricId, sbpId, isMajor: true);
            var ageId = await SeedCriterionAsync(
                "Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.Gte));

            var result = await _handler.HandleAsync(
                ScoringAdd(metricId, ageId, OnePoint()),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);
            Assert.Null(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(1, await freshContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(x => x.ClinicalMetricId == metricId, TestContext.Current.CancellationToken));
        }

        #endregion

        private async Task<Guid> SeedMetricAsync(
            (string Name, string Code, string Description, string Version, string Publisher) metric,
            bool isDeleted = false)
        {
            var entity = new ClinicalMetric
            {
                Name = metric.Name,
                Code = metric.Code,
                Description = metric.Description,
                Version = metric.Version,
                Publisher = metric.Publisher,
                IsApplied = true,
                Rules = [],
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.ClinicalMetrics.AddAsync(entity, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return entity.Id;
        }

        private async Task<Guid> SeedCriterionAsync(string name, Formula formula, bool isDeleted = false)
        {
            var criterion = new Criterion
            {
                Name = name,
                Formula = formula,
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

        private async Task<Guid> SeedScoringRuleAsync(
            Guid metricId, Guid criterionId, Formula scoreFunction, bool isDeleted = false)
        {
            /*
             * Seed through a fresh context: the handler must see only what its own
             * query returns, exactly like in production. Seeding through the handler
             * context would hand the soft deleted rule to the handler via EF
             * relationship fixup even though the query filter hides it
             */
            await using var seedContext = new ClinicalDbContext(_options);
            var rule = new ScoringRule
            {
                ClinicalMetricId = metricId,
                CriterionId = criterionId,
                ScoreFunction = scoreFunction,
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
            await seedContext.MetricRules.AddAsync(rule, TestContext.Current.CancellationToken);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            return rule.Id;
        }

        private async Task<Guid> SeedMajorMinorRuleAsync(Guid metricId, Guid criterionId, bool isMajor)
        {
            // Same reason as SeedScoringRuleAsync: keep the handler's change tracker
            // free of pre-seeded rules
            await using var seedContext = new ClinicalDbContext(_options);
            var rule = new MajorMinorRule
            {
                ClinicalMetricId = metricId,
                CriterionId = criterionId,
                IsMajor = isMajor,
            };
            await seedContext.MetricRules.AddAsync(rule, TestContext.Current.CancellationToken);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            return rule.Id;
        }

        private static AddMetricRuleCommand ScoringAdd(Guid metricId, Guid criterionId, FormulaDto scoreFunction) => new()
        {
            ClinicalMetricId = metricId,
            CriterionId = criterionId,
            ScoreFunction = scoreFunction,
        };

        private static AddMetricRuleCommand MajorMinorAdd(Guid metricId, Guid criterionId, bool isMajor) => new()
        {
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
