using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.Criteria.DeleteCriterion;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.ServiceDefaults.Models;

namespace Respira.Application.Test.Features.Criteria.DeleteCriterion
{
    public class DeleteCriterionHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly DeleteCriterionHandler _handler;
        private readonly IDbContext _context;

        public DeleteCriterionHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var logger = new Mock<ILogger<DeleteCriterionHandler>>().Object;

            // Initialize handler
            _handler = new(_context, logger);
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

        /*
         * The two criteria every scenario seeds: the target of the delete and an
         * unrelated control that must never be touched. The formulas mirror the real
         * seed data (AGE >= 65 for pneumonia severity, SBP < 90 mmHg for hypotension)
         * and both evaluate to a boolean as a criterion requires
         */
        private static Criterion CreateAgeCriterion() =>
            new("Tuổi >= 65", new BinaryFormula(
                new VariableFormula(new VariableRef(Guid.CreateVersion7(), "AGE", ClinicalValueType.Numeric)),
                new NumericConstantFormula(65),
                ExpressionOperator.GTE));

        private static Criterion CreateHypotensionCriterion() =>
            new("Huyết áp tâm thu < 90 mmHg", new BinaryFormula(
                new VariableFormula(new VariableRef(Guid.CreateVersion7(), "SBP", ClinicalValueType.Numeric)),
                new NumericConstantFormula(90),
                ExpressionOperator.LT));

        private async Task<Criterion> SeedCriterionAsync(Criterion criterion)
        {
            await _context.Criteria.AddAsync(criterion, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return criterion;
        }

        private async Task<Pathogen> SeedPathogenAsync(string name, string description, bool isAtypical)
        {
            var pathogen = new Pathogen
            {
                Name = name,
                Description = description,
                IsAtypical = isAtypical,
            };
            await _context.Pathogens.AddAsync(pathogen, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return pathogen;
        }

        private async Task<RiskFactor> SeedRiskFactorAsync(Guid pathogenId, Guid criterionId)
        {
            var factor = new RiskFactor
            {
                PathogenId = pathogenId,
                CriterionId = criterionId,
            };
            await _context.RiskFactors.AddAsync(factor, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return factor;
        }

        private async Task<ClinicalMetrics> SeedMetricsAsync(string name, string code, string description)
        {
            var metrics = new ClinicalMetrics
            {
                Name = name,
                Code = code,
                Description = description,
            };
            await _context.ClinicalMetrics.AddAsync(metrics, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return metrics;
        }

        // Every CURB-65 criterion contributes exactly 1 point, so the score function is
        // a constant 1 rather than an arbitrary number
        private async Task<MetricsRule> SeedScoringRuleAsync(Guid metricsId, Guid criterionId)
        {
            var rule = new ScoringRule
            {
                ClinicalMetricsId = metricsId,
                CriterionId = criterionId,
                ScoreFunction = new NumericConstantFormula(1),
            };
            await _context.MetricsRules.AddAsync(rule, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return rule;
        }

        private async Task<Treatment> SeedTreatmentAsync(
            Severity severity, TreatmentSite treatmentSite, params Criterion[] criteria)
        {
            var treatment = new Treatment
            {
                Severity = severity,
                TreatmentSite = treatmentSite,
                Criteria = [.. criteria],
            };
            await _context.Treatments.AddAsync(treatment, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return treatment;
        }

        private async Task AssertSoftDeletedAsync<T>(DbSet<T> set, Guid id) where T : Base
        {
            var entity = await set.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
            Assert.True(entity.IsDeleted);
            Assert.NotNull(entity.DeletedAt);
        }

        private async Task AssertNotDeletedAsync<T>(DbSet<T> set, Guid id) where T : Base
        {
            var entity = await set.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
            Assert.False(entity.IsDeleted);
            Assert.Null(entity.DeletedAt);
        }

        #region Happy path

        [Fact]
        public async Task DeleteCriterion_WithoutAssociations_Success()
        {
            // Lower boundary of the cascade: nothing references the deleted criterion
            var target = await SeedCriterionAsync(CreateAgeCriterion());
            var other = await SeedCriterionAsync(CreateHypotensionCriterion());

            var result = await _handler.HandleAsync(
                new DeleteCriterionCommand { Id = target.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            // All entities carry a !IsDeleted query filter, so IgnoreQueryFilters is
            // required to observe the soft-delete flags
            await using var freshContext = new ClinicalDbContext(_options);

            var deleted = await freshContext.Criteria.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken);
            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);

            // The row is a soft delete: it stays in the table but disappears from the
            // default query used by every other feature
            Assert.False(await freshContext.Criteria
                .AnyAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken));

            // The unrelated criterion must stay untouched
            var untouched = await freshContext.Criteria.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == other.Id, TestContext.Current.CancellationToken);
            Assert.False(untouched.IsDeleted);
            Assert.Null(untouched.DeletedAt);
        }

        [Fact]
        public async Task DeleteCriterion_SingleRiskFactor_CascadeDeleted_Success()
        {
            // Boundary: exactly one risk factor references the deleted criterion
            var target = await SeedCriterionAsync(CreateAgeCriterion());
            var other = await SeedCriterionAsync(CreateHypotensionCriterion());
            var pathogen = await SeedPathogenAsync(
                "Streptococcus pneumoniae",
                "Gram-positive diplococcus, most common cause of community-acquired pneumonia",
                isAtypical: false);
            var targetFactor = await SeedRiskFactorAsync(pathogen.Id, target.Id);
            var otherFactor = await SeedRiskFactorAsync(pathogen.Id, other.Id);

            var result = await _handler.HandleAsync(
                new DeleteCriterionCommand { Id = target.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            await AssertSoftDeletedAsync(freshContext.RiskFactors, targetFactor.Id);
            await AssertNotDeletedAsync(freshContext.RiskFactors, otherFactor.Id);

            // The pathogen is a parent of the risk factor, not a cascade target
            await AssertNotDeletedAsync(freshContext.Pathogens, pathogen.Id);
            await AssertNotDeletedAsync(freshContext.Criteria, other.Id);
        }

        [Fact]
        public async Task DeleteCriterion_ScoringRule_CascadeDeleted_Success()
        {
            // Boundary: exactly one metrics rule references the deleted criterion
            var target = await SeedCriterionAsync(CreateAgeCriterion());
            var other = await SeedCriterionAsync(CreateHypotensionCriterion());
            var metrics = await SeedMetricsAsync(
                "Severity metrics CURB-65",
                "CURB-65",
                "CURB-65 metrics to assess severity of a patient");
            var targetRule = await SeedScoringRuleAsync(metrics.Id, target.Id);
            var otherRule = await SeedScoringRuleAsync(metrics.Id, other.Id);

            var result = await _handler.HandleAsync(
                new DeleteCriterionCommand { Id = target.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            await AssertSoftDeletedAsync(freshContext.MetricsRules, targetRule.Id);
            await AssertNotDeletedAsync(freshContext.MetricsRules, otherRule.Id);

            // Only the rule cascades, the metric itself keeps living
            await AssertNotDeletedAsync(freshContext.ClinicalMetrics, metrics.Id);

            // The score function of the surviving rule survives the jsonb round trip
            var keptRule = await freshContext.MetricsRules.IgnoreQueryFilters()
                .OfType<ScoringRule>()
                .SingleAsync(x => x.Id == otherRule.Id, TestContext.Current.CancellationToken);
            Assert.Equal(
                1m, Assert.IsType<NumericConstantFormula>(keptRule.ScoreFunction).Constant);
        }

        [Fact]
        public async Task DeleteCriterion_SingleTreatment_Untouch_Success()
        {
            // Boundary: exactly one treatment protocol references the deleted criterion
            var target = await SeedCriterionAsync(CreateAgeCriterion());
            var other = await SeedCriterionAsync(CreateHypotensionCriterion());
            var targetTreatment = await SeedTreatmentAsync(
                Severity.Moderate, TreatmentSite.Inpatient, target);
            var otherTreatment = await SeedTreatmentAsync(
                Severity.Mild, TreatmentSite.Outpatient, other);

            var result = await _handler.HandleAsync(
                new DeleteCriterionCommand { Id = target.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            await AssertNotDeletedAsync(freshContext.Treatments, targetTreatment.Id);
            await AssertNotDeletedAsync(freshContext.Treatments, otherTreatment.Id);
            await AssertNotDeletedAsync(freshContext.Criteria, other.Id);
        }

        [Fact]
        public async Task DeleteCriterion_AllAssociations_UpperBoundaryCascade_Success()
        {
            /*
             * Upper boundary of the cascade: several of every associated kind at once,
             * plus a control set on a second criterion that must not be touched
             */
            var target = await SeedCriterionAsync(CreateAgeCriterion());
            var other = await SeedCriterionAsync(CreateHypotensionCriterion());
            var pathogen = await SeedPathogenAsync(
                "Klebsiella pneumoniae",
                "Gram-negative bacillus causing hospital-acquired pneumonia",
                isAtypical: true);
            var metrics = await SeedMetricsAsync(
                "Severity metrics CURB-65",
                "CURB-65",
                "CURB-65 metrics to assess severity of a patient");

            var targetFactorOne = await SeedRiskFactorAsync(pathogen.Id, target.Id);
            var targetFactorTwo = await SeedRiskFactorAsync(pathogen.Id, target.Id);
            var otherFactor = await SeedRiskFactorAsync(pathogen.Id, other.Id);

            var targetRuleOne = await SeedScoringRuleAsync(metrics.Id, target.Id);
            var targetRuleTwo = await SeedScoringRuleAsync(metrics.Id, target.Id);
            var otherRule = await SeedScoringRuleAsync(metrics.Id, other.Id);

            var targetTreatmentOne = await SeedTreatmentAsync(
                Severity.Severe, TreatmentSite.IntensiveCareUnit, target);
            var targetTreatmentTwo = await SeedTreatmentAsync(
                Severity.Moderate, TreatmentSite.Inpatient, target);
            var otherTreatment = await SeedTreatmentAsync(
                Severity.Mild, TreatmentSite.Outpatient, other);

            var result = await _handler.HandleAsync(
                new DeleteCriterionCommand { Id = target.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);

            // The criterion itself
            await AssertSoftDeletedAsync(freshContext.Criteria, target.Id);

            // Every associated row of the deleted criterion
            await AssertSoftDeletedAsync(freshContext.RiskFactors, targetFactorOne.Id);
            await AssertSoftDeletedAsync(freshContext.RiskFactors, targetFactorTwo.Id);
            await AssertSoftDeletedAsync(freshContext.MetricsRules, targetRuleOne.Id);
            await AssertSoftDeletedAsync(freshContext.MetricsRules, targetRuleTwo.Id);
            await AssertNotDeletedAsync(freshContext.Treatments, targetTreatmentOne.Id);
            await AssertNotDeletedAsync(freshContext.Treatments, targetTreatmentTwo.Id);

            Assert.Equal(2, await freshContext.RiskFactors.IgnoreQueryFilters()
                .CountAsync(x => x.CriterionId == target.Id, TestContext.Current.CancellationToken));
            Assert.Equal(2, await freshContext.MetricsRules.IgnoreQueryFilters()
                .CountAsync(x => x.CriterionId == target.Id, TestContext.Current.CancellationToken));
            Assert.Equal(2, await freshContext.Treatments.IgnoreQueryFilters()
                .CountAsync(x => x.Criteria.Any(c => c.Id == target.Id), TestContext.Current.CancellationToken));

            // The control set of the second criterion stays live
            await AssertNotDeletedAsync(freshContext.RiskFactors, otherFactor.Id);
            await AssertNotDeletedAsync(freshContext.MetricsRules, otherRule.Id);
            await AssertNotDeletedAsync(freshContext.Treatments, otherTreatment.Id);
            await AssertNotDeletedAsync(freshContext.Criteria, other.Id);

            // Parents of the cascade are never deleted themselves
            await AssertNotDeletedAsync(freshContext.Pathogens, pathogen.Id);
            await AssertNotDeletedAsync(freshContext.ClinicalMetrics, metrics.Id);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task DeleteCriterion_UnknownId_Fail()
        {
            var target = await SeedCriterionAsync(CreateAgeCriterion());
            var pathogen = await SeedPathogenAsync(
                "Streptococcus pneumoniae",
                "Gram-positive diplococcus, most common cause of community-acquired pneumonia",
                isAtypical: false);
            await SeedRiskFactorAsync(pathogen.Id, target.Id);
            var unknownId = Guid.CreateVersion7();

            var result = await _handler.HandleAsync(
                new DeleteCriterionCommand { Id = unknownId }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Nothing must be soft-deleted when the target does not exist
            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.Criteria.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken));
            Assert.Equal(0, await freshContext.RiskFactors.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken));
            Assert.Equal(0, await freshContext.MetricsRules.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken));
            Assert.Equal(0, await freshContext.Treatments.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteCriterion_EmptyId_Fail()
        {
            // Boundary: Guid.Empty can never match a stored criterion
            await SeedCriterionAsync(CreateAgeCriterion());

            var result = await _handler.HandleAsync(
                new DeleteCriterionCommand { Id = Guid.Empty }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.Criteria.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteCriterion_AlreadyDeleted_Fail()
        {
            // The !IsDeleted query filter hides soft-deleted rows, so deleting twice
            // is reported as a missing criterion
            var target = await SeedCriterionAsync(CreateAgeCriterion());
            var other = await SeedCriterionAsync(CreateHypotensionCriterion());
            var pathogen = await SeedPathogenAsync(
                "Klebsiella pneumoniae",
                "Gram-negative bacillus causing hospital-acquired pneumonia",
                isAtypical: true);
            var factor = await SeedRiskFactorAsync(pathogen.Id, target.Id);

            var firstDelete = await _handler.HandleAsync(
                new DeleteCriterionCommand { Id = target.Id }, TestContext.Current.CancellationToken);
            Assert.True(firstDelete.IsSuccess());

            // PostgreSQL truncates the timestamp to microsecond precision, so compare
            // against the value that was actually persisted
            await using var seedContext = new ClinicalDbContext(_options);
            var storedCriterionDeletedAt = (await seedContext.Criteria.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken)).DeletedAt;
            var storedFactorDeletedAt = (await seedContext.RiskFactors.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == factor.Id, TestContext.Current.CancellationToken)).DeletedAt;

            var secondDelete = await _handler.HandleAsync(
                new DeleteCriterionCommand { Id = target.Id }, TestContext.Current.CancellationToken);

            Assert.True(secondDelete.IsFailure());
            Assert.NotNull(secondDelete.Error);
            Assert.Equal(ApplicationStatus.BadRequest, secondDelete.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);

            // The handler bails out before the cascade, so the original timestamps are kept
            var criterion = await freshContext.Criteria.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken);
            Assert.True(criterion.IsDeleted);
            Assert.Equal(storedCriterionDeletedAt, criterion.DeletedAt);

            var keptFactor = await freshContext.RiskFactors.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == factor.Id, TestContext.Current.CancellationToken);
            Assert.True(keptFactor.IsDeleted);
            Assert.Equal(storedFactorDeletedAt, keptFactor.DeletedAt);

            // The unrelated criterion is untouched
            await AssertNotDeletedAsync(freshContext.Criteria, other.Id);
        }

        #endregion
    }
}
