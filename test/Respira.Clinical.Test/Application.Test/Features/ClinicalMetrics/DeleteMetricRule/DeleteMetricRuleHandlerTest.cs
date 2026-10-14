using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.ClinicalMetrics.DeleteMetricRule;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.ClinicalMetrics.DeleteMetricRule
{
    public class DeleteMetricRuleHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private static readonly (string Name, string Code, string Description, string Version, string Publisher) Curb65 =
            ("Severity metrics CURB-65", "CURB-65", "CURB-65 metrics to assess severity of a patient",
                "2002-2003", "Hiệp hội Lồng ngực Anh (BTS)");

        private static readonly (string Name, string Code, string Description, string Version, string Publisher) IdsaAts =
            ("ICU hospitalization criteria by IDSA/ATS", "IDSA/ATS", "IDSA/ATS metrics to assess whether a severe patient need ICU or not",
                "2026", "Hiệp hội Bệnh nhiễm trùng Mỹ (IDSA) và Hiệp hội Lồng ngực Mỹ (ATS)");

        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly DeleteMetricRuleHandler _handler;
        private readonly IDbContext _context;

        public DeleteMetricRuleHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var logger = new Mock<ILogger<DeleteMetricRuleHandler>>().Object;

            // Initialize handler
            _handler = new(_context, logger);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Clear leftover data (children first for FK constraints) so the count
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
        public async Task DeleteRule_Success()
        {
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync("Tuổi >= 65", "AGE", 65, ExpressionOperator.GTE);
            var ruleId = await SeedScoringRuleAsync(metricId, ageId);

            var result = await _handler.HandleAsync(
                new DeleteMetricRuleCommand { Id = ruleId }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            var saved = await FindRuleIncludingDeletedAsync(ruleId);
            Assert.NotNull(saved);

            // Business rule: a delete is a soft delete, the rule survives with a
            // deletion timestamp so its history is kept
            Assert.True(saved.IsDeleted);
            Assert.NotNull(saved.DeletedAt);
            Assert.True(saved.DeletedAt.Value >= saved.CreatedAt);

            // The payload of the rule must stay exactly as it was
            Assert.Equal(metricId, saved.ClinicalMetricId);
            Assert.Equal(ageId, saved.CriterionId);
            Assert.Equal("1", Assert.IsType<ScoringRule>(saved).ScoreFunction.ToString());

            // The default query filter must now hide the deleted rule
            await using var filteredContext = new ClinicalDbContext(_options);
            Assert.False(await filteredContext.MetricRules
                .AnyAsync(x => x.Id == ruleId, TestContext.Current.CancellationToken));
            Assert.Equal(0, await filteredContext.MetricRules
                .CountAsync(TestContext.Current.CancellationToken));
            Assert.Equal(1, await filteredContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));

            // The delete only touches the rule: its metric and criterion stay in use
            Assert.Equal(1, await filteredContext.ClinicalMetrics
                .CountAsync(TestContext.Current.CancellationToken));
            Assert.Equal(1, await filteredContext.Criteria
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteRule_MajorMinorRule_Success()
        {
            // The IDSA/ATS metric counts major and minor criteria instead of scoring points
            var metricId = await SeedMetricAsync(IdsaAts);
            var hypotensionId = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", "SBP", 90, ExpressionOperator.LT);
            var ruleId = await SeedMajorMinorRuleAsync(metricId, hypotensionId, isMajor: true);

            var result = await _handler.HandleAsync(
                new DeleteMetricRuleCommand { Id = ruleId }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            var saved = await FindRuleIncludingDeletedAsync(ruleId);
            Assert.NotNull(saved);
            Assert.True(saved.IsDeleted);
            Assert.NotNull(saved.DeletedAt);

            // The major/minor weight must survive the delete
            Assert.True(Assert.IsType<MajorMinorRule>(saved).IsMajor);

            await using var filteredContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await filteredContext.MetricRules
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteRule_OneAmongSeveral_UpperBoundary_Success()
        {
            /*
             * Upper boundary: three CURB-65 rules on one metric, the middle one is
             * removed and the two others must stay live
             */
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync("Tuổi >= 65", "AGE", 65, ExpressionOperator.GTE);
            var hypotensionId = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", "SBP", 90, ExpressionOperator.LT);
            var tachypneaId = await SeedCriterionAsync(
                "Nhịp thở >= 30 lần/phút", "RR", 30, ExpressionOperator.GTE);

            var ageRuleId = await SeedScoringRuleAsync(metricId, ageId);
            var hypotensionRuleId = await SeedScoringRuleAsync(metricId, hypotensionId);
            var tachypneaRuleId = await SeedScoringRuleAsync(metricId, tachypneaId);

            var result = await _handler.HandleAsync(
                new DeleteMetricRuleCommand { Id = hypotensionRuleId }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            Assert.True((await FindRuleIncludingDeletedAsync(hypotensionRuleId))!.IsDeleted);

            // The sibling rules of the metric stay untouched
            foreach (var siblingId in new[] { ageRuleId, tachypneaRuleId })
            {
                var sibling = await FindRuleIncludingDeletedAsync(siblingId);
                Assert.NotNull(sibling);
                Assert.False(sibling.IsDeleted);
                Assert.Null(sibling.DeletedAt);
            }

            await using var filteredContext = new ClinicalDbContext(_options);
            Assert.Equal(2, await filteredContext.MetricRules
                .CountAsync(TestContext.Current.CancellationToken));
            Assert.Equal(3, await filteredContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task DeleteRule_UnknownId_Fail()
        {
            var result = await _handler.HandleAsync(
                new DeleteMetricRuleCommand { Id = Guid.CreateVersion7() },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task DeleteRule_EmptyId_Fail()
        {
            // Boundary: no row can ever match Guid.Empty, so the lookup fails
            var result = await _handler.HandleAsync(
                new DeleteMetricRuleCommand { Id = Guid.Empty },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task DeleteRule_AlreadyDeleted_Fail()
        {
            /*
             * A soft deleted rule is hidden by the query filter, so a second delete
             * cannot find it and is rejected instead of overwriting DeletedAt
             */
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync("Tuổi >= 65", "AGE", 65, ExpressionOperator.GTE);
            var ruleId = await SeedScoringRuleAsync(metricId, ageId);

            var first = await _handler.HandleAsync(
                new DeleteMetricRuleCommand { Id = ruleId }, TestContext.Current.CancellationToken);
            Assert.True(first.IsSuccess());

            var firstRead = await FindRuleIncludingDeletedAsync(ruleId);
            Assert.NotNull(firstRead);
            var firstDeletedAt = firstRead.DeletedAt;

            var second = await _handler.HandleAsync(
                new DeleteMetricRuleCommand { Id = ruleId }, TestContext.Current.CancellationToken);

            Assert.True(second.IsFailure());
            Assert.NotNull(second.Error);
            Assert.Equal(ApplicationStatus.BadRequest, second.StatusCode);

            // The rejected call must not touch the already deleted rule
            var secondRead = await FindRuleIncludingDeletedAsync(ruleId);
            Assert.NotNull(secondRead);
            Assert.True(secondRead.IsDeleted);
            Assert.Equal(firstDeletedAt, secondRead.DeletedAt);
        }

        [Fact]
        public async Task DeleteRule_SoftDeletedRule_Fail()
        {
            // A rule deleted beforehand is reported as missing, and its payload,
            // including the deletion timestamp, must stay exactly as it was
            var metricId = await SeedMetricAsync(Curb65);
            var ageId = await SeedCriterionAsync("Tuổi >= 65", "AGE", 65, ExpressionOperator.GTE);
            var ruleId = await SeedScoringRuleAsync(metricId, ageId, isDeleted: true);

            var result = await _handler.HandleAsync(
                new DeleteMetricRuleCommand { Id = ruleId }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            var saved = await FindRuleIncludingDeletedAsync(ruleId);
            Assert.NotNull(saved);
            Assert.True(saved.IsDeleted);
            Assert.NotNull(saved.DeletedAt);
            Assert.Equal(metricId, saved.ClinicalMetricId);
            Assert.Equal(ageId, saved.CriterionId);
        }

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
        private async Task<Guid> SeedCriterionAsync(
            string name, string code, decimal threshold, ExpressionOperator op)
        {
            var criterion = new Criterion(name, new BinaryFormula(
                new VariableFormula(new VariableRef(Guid.CreateVersion7(), code, ClinicalValueType.Numeric)),
                new NumericConstantFormula(threshold),
                op));
            await _context.Criteria.AddAsync(criterion, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return criterion.Id;
        }

        private async Task<Guid> SeedScoringRuleAsync(
            Guid metricId, Guid criterionId, decimal score = 1, bool isDeleted = false)
        {
            // Every CURB-65 component is worth exactly one point, like in the seed data
            var rule = new ScoringRule
            {
                ClinicalMetricId = metricId,
                CriterionId = criterionId,
                ScoreFunction = new NumericConstantFormula(score),
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.MetricRules.AddAsync(rule, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return rule.Id;
        }

        private async Task<Guid> SeedMajorMinorRuleAsync(
            Guid metricId, Guid criterionId, bool isMajor, bool isDeleted = false)
        {
            var rule = new MajorMinorRule
            {
                ClinicalMetricId = metricId,
                CriterionId = criterionId,
                IsMajor = isMajor,
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.MetricRules.AddAsync(rule, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return rule.Id;
        }

        // Read through a context that ignores the query filters, so a soft deleted row
        // can still be inspected after the handler hid it
        private async Task<MetricRule?> FindRuleIncludingDeletedAsync(Guid id)
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.MetricRules.IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        }
    }
}
