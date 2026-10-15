using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.ClinicalMetrics.DeleteClinicalMetric;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.ClinicalMetrics.DeleteClinicalMetric
{
    public class DeleteClinicalMetricHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private const string Curb65Code = "CURB-65";
        private const string IdsaAtsCode = "IDSA/ATS";
        private const string PsiCode = "PSI";

        // The metrics of the real seed data (seed-data.jsonc)
        private static readonly (string Name, string Code, string Description, string Version, string Publisher) Curb65 =
            ("Severity metrics CURB-65", Curb65Code, "CURB-65 metrics to assess severity of a patient",
                "2002-2003", "Hiệp hội Lồng ngực Anh (BTS)");

        private static readonly (string Name, string Code, string Description, string Version, string Publisher) Psi =
            ("Pneumonia Severity Index (PSI)", PsiCode, "PSI metrics to assess severity of a patient with more detail than CURB-65",
                "1997", "Đội ngũ Nghiên cứu Kết quả Điều trị Viêm phổi (PORT - Pneumonia Outcomes Research Team)");

        private static readonly (string Name, string Code, string Description, string Version, string Publisher) IdsaAts =
            ("ICU hospitalization criteria by IDSA/ATS", IdsaAtsCode, "IDSA/ATS metrics to assess whether a severe patient need ICU or not",
                "2026", "Hiệp hội Bệnh nhiễm trùng Mỹ (IDSA) và Hiệp hội Lồng ngực Mỹ (ATS)");

        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly DeleteClinicalMetricHandler _handler;
        private readonly IDbContext _context;

        public DeleteClinicalMetricHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var logger = new Mock<ILogger<DeleteClinicalMetricHandler>>().Object;

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
        public async Task DeleteClinicalMetric_Success()
        {
            // Lower boundary: a metric without any rule, so the cascade removes nothing
            var metricId = await SeedMetricAsync(Curb65);

            var result = await _handler.HandleAsync(new DeleteClinicalMetricCommand
            {
                Id = metricId,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            var saved = await FindMetricIncludingDeletedAsync(metricId);
            Assert.NotNull(saved);

            // Business rule: a delete is a soft delete, the metric survives with a
            // deletion timestamp so its history is kept
            Assert.True(saved.IsDeleted);
            Assert.NotNull(saved.DeletedAt);
            Assert.True(saved.DeletedAt.Value >= saved.CreatedAt);
            Assert.Equal(Curb65Code, saved.Code);

            // The default query filter must now hide the deleted metric
            await using var filteredContext = new ClinicalDbContext(_options);
            Assert.False(await filteredContext.ClinicalMetrics
                .AnyAsync(x => x.Id == metricId, TestContext.Current.CancellationToken));
            Assert.Equal(0, await filteredContext.ClinicalMetrics
                .CountAsync(TestContext.Current.CancellationToken));
            Assert.Equal(1, await filteredContext.ClinicalMetrics.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteClinicalMetric_CascadeDeletesAllScoringRules_Success()
        {
            /*
             * Business rule: the delete cascades to every rule of the metric inside one
             * transaction, soft deleting each rule with its own deletion timestamp
             */
            var metricId = await SeedMetricAsync(Curb65);
            var ruleIds = await SeedCurb65RulesAsync(metricId);

            var result = await _handler.HandleAsync(new DeleteClinicalMetricCommand
            {
                Id = metricId,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            var saved = await FindMetricIncludingDeletedAsync(metricId);
            Assert.NotNull(saved);
            Assert.True(saved.IsDeleted);
            Assert.NotNull(saved.DeletedAt);

            var rules = await FindRulesIncludingDeletedAsync(metricId);
            Assert.Equal(5, rules.Count);
            Assert.Equal(ruleIds.Order(), rules.Select(x => x.Id).Order());
            Assert.All(rules, x => Assert.True(x.IsDeleted));
            Assert.All(rules, x => Assert.NotNull(x.DeletedAt));
            Assert.All(rules, x => Assert.True(x.DeletedAt!.Value >= x.CreatedAt));

            // The default query filters must now hide the metric and all of its rules
            await using var filteredContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await filteredContext.ClinicalMetrics
                .CountAsync(TestContext.Current.CancellationToken));
            Assert.Equal(0, await filteredContext.MetricRules
                .CountAsync(TestContext.Current.CancellationToken));
            Assert.Equal(5, await filteredContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteClinicalMetric_CascadeDeletesMajorMinorRules_Success()
        {
            // The IDSA/ATS metric counts major and minor criteria instead of scoring points
            var metricId = await SeedMetricAsync(IdsaAts);
            var hypotensionId = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", "SBP", 90, ExpressionOperator.Lt);
            var confusionId = await SeedConfusionCriterionAsync();
            await SeedMajorMinorRuleAsync(metricId, hypotensionId, isMajor: true);
            await SeedMajorMinorRuleAsync(metricId, confusionId, isMajor: false);

            var result = await _handler.HandleAsync(new DeleteClinicalMetricCommand
            {
                Id = metricId,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            Assert.True((await FindMetricIncludingDeletedAsync(metricId))!.IsDeleted);

            var rules = await FindRulesIncludingDeletedAsync(metricId);
            Assert.Equal(2, rules.Count);
            Assert.All(rules, x => Assert.True(x.IsDeleted));
            Assert.All(rules, x => Assert.NotNull(x.DeletedAt));

            await using var filteredContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await filteredContext.MetricRules
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteClinicalMetric_CascadeOnlyTargetsOwnRules_Success()
        {
            /*
             * Business rule: the cascade covers the rules of the deleted metric only.
             * The criteria are shared entities referenced by both metrics, so they must
             * survive together with the rules of the metric that stays in use
             */
            var ageCriterionId = await SeedCriterionAsync(
                "Tuổi >= 65", "AGE", 65, ExpressionOperator.Gte);
            var sbpCriterionId = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", "SBP", 90, ExpressionOperator.Lt);

            var curb65Id = await SeedMetricAsync(Curb65);
            await SeedScoringRuleAsync(curb65Id, ageCriterionId);
            await SeedScoringRuleAsync(curb65Id, sbpCriterionId);

            var idsaAtsId = await SeedMetricAsync(IdsaAts);
            await SeedMajorMinorRuleAsync(idsaAtsId, ageCriterionId, isMajor: false);
            await SeedMajorMinorRuleAsync(idsaAtsId, sbpCriterionId, isMajor: true);

            var result = await _handler.HandleAsync(new DeleteClinicalMetricCommand
            {
                Id = curb65Id,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            Assert.True((await FindMetricIncludingDeletedAsync(curb65Id))!.IsDeleted);
            var deletedRules = await FindRulesIncludingDeletedAsync(curb65Id);
            Assert.Equal(2, deletedRules.Count);
            Assert.All(deletedRules, x => Assert.True(x.IsDeleted));

            // The other metric and its rules stay untouched
            var sibling = await FindMetricIncludingDeletedAsync(idsaAtsId);
            Assert.NotNull(sibling);
            Assert.False(sibling.IsDeleted);
            Assert.Null(sibling.DeletedAt);

            var siblingRules = await FindRulesIncludingDeletedAsync(idsaAtsId);
            Assert.Equal(2, siblingRules.Count);
            Assert.All(siblingRules, x => Assert.False(x.IsDeleted));
            Assert.All(siblingRules, x => Assert.Null(x.DeletedAt));

            // The referenced criteria are not part of the cascade
            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(2, await freshContext.Criteria
                .CountAsync(TestContext.Current.CancellationToken));
            Assert.Equal(2, await freshContext.Criteria.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteClinicalMetric_OneAmongSeveral_UpperBoundary_Success()
        {
            // Upper boundary: three metrics, the middle one is removed and the two
            // others must stay live
            var curb65Id = await SeedMetricAsync(Curb65);
            var psiId = await SeedMetricAsync(Psi);
            var idsaAtsId = await SeedMetricAsync(IdsaAts);

            var result = await _handler.HandleAsync(new DeleteClinicalMetricCommand
            {
                Id = psiId,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            Assert.True((await FindMetricIncludingDeletedAsync(psiId))!.IsDeleted);
            Assert.False((await FindMetricIncludingDeletedAsync(curb65Id))!.IsDeleted);
            Assert.False((await FindMetricIncludingDeletedAsync(idsaAtsId))!.IsDeleted);

            await using var filteredContext = new ClinicalDbContext(_options);
            Assert.Equal(2, await filteredContext.ClinicalMetrics
                .CountAsync(TestContext.Current.CancellationToken));
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task DeleteClinicalMetric_UnknownId_Fail()
        {
            var result = await _handler.HandleAsync(new DeleteClinicalMetricCommand
            {
                Id = Guid.CreateVersion7(),
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task DeleteClinicalMetric_EmptyId_Fail()
        {
            // Boundary: no row can ever match Guid.Empty, so the lookup fails
            var result = await _handler.HandleAsync(new DeleteClinicalMetricCommand
            {
                Id = Guid.Empty,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task DeleteClinicalMetric_AlreadyDeleted_Fail()
        {
            /*
             * A soft deleted metric is hidden by the query filter, so a second delete
             * cannot find it and is rejected instead of overwriting DeletedAt
             */
            var metricId = await SeedMetricAsync(Curb65);
            await SeedCurb65RulesAsync(metricId);

            var first = await _handler.HandleAsync(new DeleteClinicalMetricCommand
            {
                Id = metricId,
            }, TestContext.Current.CancellationToken);
            Assert.True(first.IsSuccess());

            var firstRead = await FindMetricIncludingDeletedAsync(metricId);
            Assert.NotNull(firstRead);
            var firstDeletedAt = firstRead.DeletedAt;

            var second = await _handler.HandleAsync(new DeleteClinicalMetricCommand
            {
                Id = metricId,
            }, TestContext.Current.CancellationToken);

            Assert.True(second.IsFailure());
            Assert.NotNull(second.Error);
            Assert.Equal(ApplicationStatus.BadRequest, second.StatusCode);

            // The rejected call must not touch the metric nor its already deleted rules
            var secondRead = await FindMetricIncludingDeletedAsync(metricId);
            Assert.NotNull(secondRead);
            Assert.True(secondRead.IsDeleted);
            Assert.Equal(firstDeletedAt, secondRead.DeletedAt);

            var rules = await FindRulesIncludingDeletedAsync(metricId);
            Assert.Equal(5, rules.Count);
            Assert.All(rules, x => Assert.True(x.IsDeleted));
        }

        [Fact]
        public async Task DeleteClinicalMetric_SoftDeletedMetric_Fail()
        {
            // A metric deleted beforehand is reported as missing, and its payload,
            // including the deletion timestamp, must stay exactly as it was
            var metricId = await SeedMetricAsync(Curb65, isDeleted: true);

            var result = await _handler.HandleAsync(new DeleteClinicalMetricCommand
            {
                Id = metricId,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            var saved = await FindMetricIncludingDeletedAsync(metricId);
            Assert.NotNull(saved);
            Assert.True(saved.IsDeleted);
            Assert.NotNull(saved.DeletedAt);
            Assert.Equal(Curb65.Name, saved.Name);
        }

        #endregion

        private async Task<Guid> SeedMetricAsync(
            (string Name, string Code, string Description, string Version, string Publisher) metric,
            bool isApplied = true,
            bool isDeleted = false)
        {
            var entity = new ClinicalMetric(
                metric.Name,
                metric.Code,
                metric.Description,
                metric.Version,
                metric.Publisher,
                isApplied,
                [])
            {
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };

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

        // The new confusion of CURB-65 and IDSA/ATS is a plain boolean variable
        private async Task<Guid> SeedConfusionCriterionAsync()
        {
            var criterion = new Criterion("Thay đổi tri giác", new VariableFormula(
                new VariableRef(Guid.CreateVersion7(), "CONFUSION", ClinicalValueType.Boolean)));
            await _context.Criteria.AddAsync(criterion, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return criterion.Id;
        }

        private async Task<Guid> SeedScoringRuleAsync(Guid metricId, Guid criterionId, decimal score = 1)
        {
            // Every CURB-65 component is worth exactly one point, like in the seed data
            var rule = new ScoringRule
            {
                ClinicalMetricId = metricId,
                CriterionId = criterionId,
                ScoreFunction = new NumericConstantFormula(score),
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

        // The five CURB-65 components of the real seed data, each worth one point
        private async Task<List<Guid>> SeedCurb65RulesAsync(Guid metricId)
        {
            var criteria = new[]
            {
                await SeedCriterionAsync("Tuổi >= 65", "AGE", 65, ExpressionOperator.Gte),
                await SeedCriterionAsync("Huyết áp tâm thu < 90 mmHg", "SBP", 90, ExpressionOperator.Lt),
                await SeedCriterionAsync("Nhịp thở >= 30 lần/phút", "RR", 30, ExpressionOperator.Gte),
                await SeedCriterionAsync("Urea máu >= 19 mg/dL", "BUN", 19, ExpressionOperator.Gte),
                await SeedConfusionCriterionAsync(),
            };

            var ruleIds = new List<Guid>();
            foreach (var criterionId in criteria)
            {
                ruleIds.Add(await SeedScoringRuleAsync(metricId, criterionId));
            }

            return ruleIds;
        }

        // Read through a context that ignores the query filters, so a soft deleted row
        // can still be inspected after the handler hid it
        private async Task<ClinicalMetric?> FindMetricIncludingDeletedAsync(Guid id)
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.ClinicalMetrics.IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        }

        private async Task<List<MetricRule>> FindRulesIncludingDeletedAsync(Guid metricId)
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.MetricRules.IgnoreQueryFilters()
                .Where(x => x.ClinicalMetricId == metricId)
                .ToListAsync(TestContext.Current.CancellationToken);
        }
    }
}
