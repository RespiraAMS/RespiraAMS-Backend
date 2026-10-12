using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.ClinicalMetrics.GetClinicalMetricById;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.ClinicalMetrics.GetClinicalMetricById
{
    public class GetClinicalMetricByIdHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private const string Curb65Name = "Severity metrics CURB-65";
        private const string Curb65Code = "CURB-65";
        private const string Curb65Description = "CURB-65 metrics to assess severity of a patient";
        private const string Curb65Version = "2002-2003";
        private const string Curb65Publisher = "Hiệp hội Lồng ngực Anh (BTS)";

        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly GetClinicalMetricByIdHandler _handler;
        private readonly IDbContext _context;

        public GetClinicalMetricByIdHandlerTest(PostgresFixture fixture)
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
        public async Task GetClinicalMetricById_ProjectsScoringRules_Success()
        {
            var metricId = await SeedMetricAsync(
                Curb65Name, Curb65Code, Curb65Description, Curb65Version, Curb65Publisher);

            // The five CURB-65 components of the real seed data, each worth one point
            var age = await SeedCriterionAsync("Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.GTE));
            var hypotension = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", Numeric("SBP", 90, ExpressionOperator.LT));
            var tachypnea = await SeedCriterionAsync(
                "Nhịp thở >= 30 lần/phút", Numeric("RR", 30, ExpressionOperator.GTE));
            var elevatedBun = await SeedCriterionAsync(
                "Urea máu >= 19 mg/dL", Numeric("BUN", 19, ExpressionOperator.GTE));
            var confusion = await SeedCriterionAsync("Thay đổi tri giác", Confusion());

            var onePoint = new NumericConstantFormula(1);
            var ruleIds = new[]
            {
                await SeedScoringRuleAsync(metricId, age, onePoint),
                await SeedScoringRuleAsync(metricId, hypotension, onePoint),
                await SeedScoringRuleAsync(metricId, tachypnea, onePoint),
                await SeedScoringRuleAsync(metricId, elevatedBun, onePoint),
                await SeedScoringRuleAsync(metricId, confusion, onePoint),
            };

            var result = await _handler.HandleAsync(
                new GetClinicalMetricByIdQuery { Id = metricId }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            // Business rule: the read exposes the whole metric, so the client knows
            // which criteria build the score and how the metric identifies itself
            Assert.Equal(metricId, result.Data.Id);
            Assert.Equal(Curb65Name, result.Data.Name);
            Assert.Equal(Curb65Code, result.Data.Code);
            Assert.Equal(Curb65Description, result.Data.Description);
            Assert.Equal(Curb65Version, result.Data.Version);
            Assert.Equal(Curb65Publisher, result.Data.Publisher);
            Assert.True(result.Data.IsApplied);

            // Business rule: every rule is rendered with its criterion and its score
            // function, so the scoring system can be evaluated from the read alone
            Assert.Equal(5, result.Data.Rules.Count());
            Assert.Equal(ruleIds.Order(), result.Data.Rules.Select(x => x.Id).Order());
            Assert.All(result.Data.Rules, x => Assert.NotEqual(Guid.Empty, x.Id));
            Assert.All(result.Data.Rules, x => Assert.Equal("1", x.ScoreFunction));
            Assert.All(result.Data.Rules, x => Assert.Null(x.IsMajorRule));

            var expectedFormulas = new Dictionary<string, string>
            {
                ["Tuổi >= 65"] = "AGE ≥ 65",
                ["Huyết áp tâm thu < 90 mmHg"] = "SBP < 90",
                ["Nhịp thở >= 30 lần/phút"] = "RR ≥ 30",
                ["Urea máu >= 19 mg/dL"] = "BUN ≥ 19",
                ["Thay đổi tri giác"] = "CONFUSION",
            };
            Assert.All(result.Data.Rules, x => Assert.Equal(
                expectedFormulas[x.Criterion.Name], x.Criterion.Formula));
        }

        [Fact]
        public async Task GetClinicalMetricById_ScoreFunctionVariants_Success()
        {
            /*
             * Business rule: the score function is rendered back from its stored JSONB
             * payload. The real PSI seed mixes the three shapes a scoring rule can take
             */
            var metricId = await SeedMetricAsync(
                "Pneumonia Severity Index (PSI)",
                "PSI",
                "PSI metrics to assess severity of a patient with more detail than CURB-65",
                "1997",
                "Đội ngũ Nghiên cứu Kết quả Điều trị Viêm phổi (PORT - Pneumonia Outcomes Research Team)");

            var age = await SeedCriterionAsync("Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.GTE));

            await SeedScoringRuleAsync(metricId, age, new NumericConstantFormula(30));
            await SeedScoringRuleAsync(metricId, age, Variable("AGE"));
            await SeedScoringRuleAsync(metricId, age, new BinaryFormula(
                Variable("AGE"), new NumericConstantFormula(10), ExpressionOperator.SUB));

            var result = await _handler.HandleAsync(
                new GetClinicalMetricByIdQuery { Id = metricId }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(3, result.Data.Rules.Count());

            // Constant, variable and arithmetic score functions; the arithmetic one
            // renders with the mathematical minus sign of the SUB operator
            string?[] expectedScoreFunctions = ["30", "AGE", "AGE − 10"];
            Assert.Equal(
                expectedScoreFunctions.Order(),
                result.Data.Rules.Select(x => x.ScoreFunction).Order());
            Assert.All(result.Data.Rules, x => Assert.Null(x.IsMajorRule));
        }

        [Fact]
        public async Task GetClinicalMetricById_MajorMinorRules_Success()
        {
            // The IDSA/ATS metric counts major and minor criteria instead of scoring points
            var metricId = await SeedMetricAsync(
                "ICU hospitalization criteria by IDSA/ATS",
                "IDSA/ATS",
                "IDSA/ATS metrics to assess whether a severe patient need ICU or not",
                "2026",
                "Hiệp hội Bệnh nhiễm trùng Mỹ (IDSA) và Hiệp hội Lồng ngực Mỹ (ATS)");

            var hypotension = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", Numeric("SBP", 90, ExpressionOperator.LT));
            var confusion = await SeedCriterionAsync("Thay đổi tri giác", Confusion());
            var age = await SeedCriterionAsync("Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.GTE));
            var elevatedBun = await SeedCriterionAsync(
                "Urea máu >= 19 mg/dL", Numeric("BUN", 19, ExpressionOperator.GTE));

            await SeedMajorMinorRuleAsync(metricId, hypotension, isMajor: true);
            await SeedMajorMinorRuleAsync(metricId, confusion, isMajor: true);
            await SeedMajorMinorRuleAsync(metricId, age, isMajor: false);
            await SeedMajorMinorRuleAsync(metricId, elevatedBun, isMajor: false);

            var result = await _handler.HandleAsync(
                new GetClinicalMetricByIdQuery { Id = metricId }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            Assert.Equal(4, result.Data.Rules.Count());

            // Business rule: a major/minor rule carries no score, only its weight
            Assert.All(result.Data.Rules, x => Assert.Null(x.ScoreFunction));

            var majorRules = result.Data.Rules.Where(x => x.IsMajorRule == true).ToList();
            var minorRules = result.Data.Rules.Where(x => x.IsMajorRule == false).ToList();
            Assert.Equal(2, majorRules.Count);
            Assert.Equal(2, minorRules.Count);
            Assert.Equal(
                ["Huyết áp tâm thu < 90 mmHg", "Thay đổi tri giác"],
                [.. majorRules.Select(x => x.Criterion.Name).Order()]);
            Assert.Equal(
                ["Tuổi >= 65", "Urea máu >= 19 mg/dL"],
                [.. minorRules.Select(x => x.Criterion.Name).Order()]);
        }

        [Fact]
        public async Task GetClinicalMetricById_MetricWithoutRules_Success()
        {
            // Boundary: a metric whose rules were never composed projects an empty list
            var metricId = await SeedMetricAsync(
                "Severity metrics CRB-65",
                "CRB-65",
                "CRB-65 metrics to assess severity of a patient without the blood urea nitrogen component",
                Curb65Version,
                Curb65Publisher);

            var result = await _handler.HandleAsync(
                new GetClinicalMetricByIdQuery { Id = metricId }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            Assert.Equal("CRB-65", result.Data.Code);
            Assert.Empty(result.Data.Rules);
        }

        public static readonly TheoryData<bool> AppliedFlags =
        [
            // Boundary values of the IsApplied flag
            true,
            false,
        ];

        [Theory]
        [MemberData(nameof(AppliedFlags))]
        public async Task GetClinicalMetricById_IsAppliedProjected_Success(bool isApplied)
        {
            var metricId = await SeedMetricAsync(
                Curb65Name, Curb65Code, Curb65Description, Curb65Version, Curb65Publisher, isApplied);

            var result = await _handler.HandleAsync(
                new GetClinicalMetricByIdQuery { Id = metricId }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            Assert.Equal(metricId, result.Data.Id);
            Assert.Equal(isApplied, result.Data.IsApplied);
        }

        [Fact]
        public async Task GetClinicalMetricById_SoftDeletedRuleExcluded_Success()
        {
            // A rule dropped from the metric is hidden by the query filter, so the read
            // only lists the rules still in use
            var metricId = await SeedMetricAsync(
                Curb65Name, Curb65Code, Curb65Description, Curb65Version, Curb65Publisher);
            var age = await SeedCriterionAsync("Tuổi >= 65", Numeric("AGE", 65, ExpressionOperator.GTE));
            var hypotension = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", Numeric("SBP", 90, ExpressionOperator.LT));

            var liveRuleId = await SeedScoringRuleAsync(metricId, age, new NumericConstantFormula(1));
            await SeedScoringRuleAsync(
                metricId, hypotension, new NumericConstantFormula(1), isDeleted: true);

            var result = await _handler.HandleAsync(
                new GetClinicalMetricByIdQuery { Id = metricId }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            var rule = Assert.Single(result.Data.Rules);
            Assert.Equal(liveRuleId, rule.Id);
            Assert.Equal(age, rule.Criterion.Id);
            Assert.Equal("AGE ≥ 65", rule.Criterion.Formula);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task GetClinicalMetricById_UnknownId_Fail()
        {
            var result = await _handler.HandleAsync(
                new GetClinicalMetricByIdQuery { Id = Guid.CreateVersion7() },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetClinicalMetricById_EmptyId_Fail()
        {
            // Boundary: Guid.Empty can never match a stored metric
            var result = await _handler.HandleAsync(
                new GetClinicalMetricByIdQuery { Id = Guid.Empty },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetClinicalMetricById_SoftDeletedMetric_Fail()
        {
            // The !IsDeleted query filter hides soft deleted rows, so reading one is
            // reported exactly like an unknown id
            var metricId = await SeedMetricAsync(
                Curb65Name, Curb65Code, Curb65Description, Curb65Version, Curb65Publisher,
                isDeleted: true);

            var result = await _handler.HandleAsync(
                new GetClinicalMetricByIdQuery { Id = metricId },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
            Assert.Null(result.Data);
        }

        #endregion

        private async Task<Guid> SeedMetricAsync(
            string name,
            string code,
            string description,
            string version,
            string publisher,
            bool isApplied = true,
            bool isDeleted = false)
        {
            var metric = new ClinicalMetric(name, code, description, version, publisher, isApplied, [])
            {
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.ClinicalMetrics.AddAsync(metric, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return metric.Id;
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
