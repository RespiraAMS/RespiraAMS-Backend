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

namespace Respira.Clinical.Application.Test.Features.Criteria.DeleteCriterion
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
            await _context.MetricRules.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.RiskFactors.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.TreatmentProtocols.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.ClinicalMetrics.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Pathogens.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Criteria.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        #region Seed helpers

        /*
         * The two criteria every scenario seeds: the target of the delete and an
         * unrelated control that must never be touched. The formulas mirror the real
         * seed data (AGE >= 65 for pneumonia severity, SBP < 90 mmHg for hypotension)
         * and both evaluate to a boolean as a criterion requires
         */
        private static Criterion CreateAgeCriterion() => new()
        {
            Name = "Tuổi >= 65",
            Formula = new BinaryFormula(
                new VariableFormula(new VariableRef(Guid.CreateVersion7(), "AGE", ClinicalValueType.Numeric)),
                new NumericConstantFormula(65),
                ExpressionOperator.Gte)
        };

        private static Criterion CreateHypotensionCriterion() => new()
        {
            Name = "Huyết áp tâm thu < 90 mmHg",
            Formula = new BinaryFormula(
                new VariableFormula(new VariableRef(Guid.CreateVersion7(), "SBP", ClinicalValueType.Numeric)),
                new NumericConstantFormula(90),
                ExpressionOperator.Lt)
        };

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

        private async Task<RiskFactor> SeedRiskFactorAsync(
            Guid pathogenId, Guid criterionId, bool isCritical = false, bool softDeleted = false)
        {
            var factor = new RiskFactor
            {
                PathogenId = pathogenId,
                CriterionId = criterionId,
                IsCriticalFactor = isCritical,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow.AddDays(-1) : null,
            };
            await _context.RiskFactors.AddAsync(factor, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return factor;
        }

        private async Task<ClinicalMetric> SeedMetricAsync(string name, string code, string description)
        {
            var metrics = new ClinicalMetric
            {
                Name = name,
                Code = code,
                Description = description,
                Publisher = string.Empty,
                Version = string.Empty,
                IsApplied = true,
                Rules = [],
            };
            await _context.ClinicalMetrics.AddAsync(metrics, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return metrics;
        }

        // Every CURB-65 criterion contributes exactly 1 point, so the score function is
        // a constant 1 rather than an arbitrary number
        private async Task<ScoringRule> SeedScoringRuleAsync(
            Guid metricsId, Guid criterionId, bool softDeleted = false)
        {
            var rule = new ScoringRule
            {
                ClinicalMetricId = metricsId,
                CriterionId = criterionId,
                ScoreFunction = new NumericConstantFormula(1),
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow.AddDays(-1) : null,
            };
            await _context.MetricRules.AddAsync(rule, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return rule;
        }

        // A major/minor rule is the second concrete metric rule type, so it exercises the
        // same MetricRules check through the TPH discriminator
        private async Task<MajorMinorRule> SeedMajorMinorRuleAsync(
            Guid metricsId, Guid criterionId, bool isMajor, bool softDeleted = false)
        {
            var rule = new MajorMinorRule
            {
                ClinicalMetricId = metricsId,
                CriterionId = criterionId,
                IsMajor = isMajor,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow.AddDays(-1) : null,
            };
            await _context.MetricRules.AddAsync(rule, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return rule;
        }

        private async Task<TreatmentProtocol> SeedTreatmentAsync(
            Severity severity,
            TreatmentSite treatmentSite,
            Criterion[] criteria,
            bool softDeleted = false)
        {
            var treatment = new TreatmentProtocol
            {
                Severity = severity,
                TreatmentSite = treatmentSite,
                Criteria = [.. criteria],
                Publisher = string.Empty,
                Version = string.Empty,
                IsApplied = true,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow.AddDays(-1) : null,
            };
            await _context.TreatmentProtocols.AddAsync(treatment, TestContext.Current.CancellationToken);
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

        private async Task AssertNothingSoftDeletedAsync()
        {
            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.Criteria.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken));
            Assert.Equal(0, await freshContext.RiskFactors.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken));
            Assert.Equal(0, await freshContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken));
            Assert.Equal(0, await freshContext.TreatmentProtocols.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken));
        }

        #endregion

        #region Happy path

        [Fact]
        public async Task DeleteCriterion_WithoutAssociations_Success()
        {
            // A criterion that no risk factor, metric rule or treatment protocol mentions
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
        public async Task DeleteCriterion_OnlySoftDeletedReferences_Success()
        {
            /*
             * Soft-deleted risk factors, metric rules and treatment protocols are hidden
             * by the query filter, so they are no longer part of the clinical knowledge
             * base and must not forbid the delete. Their own deletion timestamps and their
             * links to the criterion must stay untouched
             */
            var target = await SeedCriterionAsync(CreateAgeCriterion());
            var other = await SeedCriterionAsync(CreateHypotensionCriterion());
            var pathogen = await SeedPathogenAsync(
                "Streptococcus pneumoniae",
                "Gram-positive diplococcus, most common cause of community-acquired pneumonia",
                isAtypical: false);
            var metrics = await SeedMetricAsync(
                "Severity metrics CURB-65",
                "CURB-65",
                "CURB-65 metrics to assess severity of a patient");

            var deletedFactor = await SeedRiskFactorAsync(
                pathogen.Id, target.Id, softDeleted: true);
            var deletedRule = await SeedScoringRuleAsync(
                metrics.Id, target.Id, softDeleted: true);
            var deletedTreatment = await SeedTreatmentAsync(
                Severity.Severe, TreatmentSite.IntensiveCareUnit, [target], softDeleted: true);

            var factorDeletedAt = (await LoadRiskFactorAsync(deletedFactor.Id)).DeletedAt;
            var ruleDeletedAt = (await LoadMetricRuleAsync(deletedRule.Id)).DeletedAt;
            var treatmentDeletedAt = (await LoadTreatmentAsync(deletedTreatment.Id)).DeletedAt;

            var result = await _handler.HandleAsync(
                new DeleteCriterionCommand { Id = target.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);
            await AssertSoftDeletedAsync(_context.Criteria, target.Id);

            // The already deleted references keep their own timestamps and stay linked
            var keptFactor = await LoadRiskFactorAsync(deletedFactor.Id);
            Assert.True(keptFactor.IsDeleted);
            Assert.Equal(factorDeletedAt, keptFactor.DeletedAt);
            Assert.Equal(target.Id, keptFactor.CriterionId);

            var keptRule = await LoadMetricRuleAsync(deletedRule.Id);
            Assert.True(keptRule.IsDeleted);
            Assert.Equal(ruleDeletedAt, keptRule.DeletedAt);
            Assert.Equal(target.Id, keptRule.CriterionId);

            var keptTreatment = await LoadTreatmentAsync(deletedTreatment.Id);
            Assert.True(keptTreatment.IsDeleted);
            Assert.Equal(treatmentDeletedAt, keptTreatment.DeletedAt);
            Assert.Contains(keptTreatment.Criteria, x => x.Id == target.Id);

            // Parents and the control criterion are never deleted
            await AssertNotDeletedAsync(_context.Pathogens, pathogen.Id);
            await AssertNotDeletedAsync(_context.ClinicalMetrics, metrics.Id);
            await AssertNotDeletedAsync(_context.Criteria, other.Id);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task DeleteCriterion_ReferencedByRiskFactor_Fail()
        {
            // Business rule: a live risk factor is evaluated with this criterion
            var target = await SeedCriterionAsync(CreateAgeCriterion());
            var pathogen = await SeedPathogenAsync(
                "Streptococcus pneumoniae",
                "Gram-positive diplococcus, most common cause of community-acquired pneumonia",
                isAtypical: false);
            var factor = await SeedRiskFactorAsync(pathogen.Id, target.Id);

            var result = await _handler.HandleAsync(
                new DeleteCriterionCommand { Id = target.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);

            // Nothing may be soft deleted when the rule is violated
            await AssertNothingSoftDeletedAsync();
            await AssertNotDeletedAsync(_context.Criteria, target.Id);
            await AssertNotDeletedAsync(_context.RiskFactors, factor.Id);
        }

        [Fact]
        public async Task DeleteCriterion_ReferencedByScoringRule_Fail()
        {
            // Business rule: a scoring metric evaluates this criterion
            var target = await SeedCriterionAsync(CreateAgeCriterion());
            var metrics = await SeedMetricAsync(
                "Severity metrics CURB-65",
                "CURB-65",
                "CURB-65 metrics to assess severity of a patient");
            var rule = await SeedScoringRuleAsync(metrics.Id, target.Id);

            var result = await _handler.HandleAsync(
                new DeleteCriterionCommand { Id = target.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);

            await AssertNothingSoftDeletedAsync();
            await AssertNotDeletedAsync(_context.Criteria, target.Id);
            await AssertNotDeletedAsync(_context.MetricRules, rule.Id);
        }

        [Fact]
        public async Task DeleteCriterion_ReferencedByMajorMinorRule_Fail()
        {
            // The second concrete metric rule type must be caught by the same rule
            var target = await SeedCriterionAsync(CreateHypotensionCriterion());
            var metrics = await SeedMetricAsync(
                "ICU hospitalization criteria by IDSA/ATS",
                "IDSA/ATS",
                "IDSA/ATS metrics to assess whether a severe patient need ICU or not");
            var rule = await SeedMajorMinorRuleAsync(metrics.Id, target.Id, isMajor: true);

            var result = await _handler.HandleAsync(
                new DeleteCriterionCommand { Id = target.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);

            await AssertNothingSoftDeletedAsync();
            await AssertNotDeletedAsync(_context.Criteria, target.Id);
            await AssertNotDeletedAsync(_context.MetricRules, rule.Id);
        }

        [Fact]
        public async Task DeleteCriterion_ReferencedByTreatmentProtocol_Fail()
        {
            // Business rule: the protocol's applicability depends on this criterion
            var target = await SeedCriterionAsync(CreateAgeCriterion());
            var treatment = await SeedTreatmentAsync(
                Severity.Moderate, TreatmentSite.Inpatient, [target]);

            var result = await _handler.HandleAsync(
                new DeleteCriterionCommand { Id = target.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);

            await AssertNothingSoftDeletedAsync();

            // The protocol is a strong entity, so it must not be cascaded or removed
            await AssertNotDeletedAsync(_context.TreatmentProtocols, treatment.Id);
            await AssertNotDeletedAsync(_context.Criteria, target.Id);

            var kept = await LoadTreatmentAsync(treatment.Id);
            Assert.Contains(kept.Criteria, x => x.Id == target.Id);
        }

        [Fact]
        public async Task DeleteCriterion_ReferencedByAll_Fail()
        {
            // Upper boundary of the rule: every kind of reference exists at once
            var target = await SeedCriterionAsync(CreateAgeCriterion());
            var pathogen = await SeedPathogenAsync(
                "Klebsiella pneumoniae",
                "Gram-negative bacillus causing hospital-acquired pneumonia",
                isAtypical: true);
            var metrics = await SeedMetricAsync(
                "Severity metrics CURB-65",
                "CURB-65",
                "CURB-65 metrics to assess severity of a patient");

            var factor = await SeedRiskFactorAsync(pathogen.Id, target.Id);
            var rule = await SeedScoringRuleAsync(metrics.Id, target.Id);
            var treatment = await SeedTreatmentAsync(
                Severity.Severe, TreatmentSite.IntensiveCareUnit, [target]);

            var result = await _handler.HandleAsync(
                new DeleteCriterionCommand { Id = target.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);

            await AssertNothingSoftDeletedAsync();
            await AssertNotDeletedAsync(_context.Criteria, target.Id);
            await AssertNotDeletedAsync(_context.RiskFactors, factor.Id);
            await AssertNotDeletedAsync(_context.MetricRules, rule.Id);
            await AssertNotDeletedAsync(_context.TreatmentProtocols, treatment.Id);
        }

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
            await AssertNothingSoftDeletedAsync();
            await AssertNotDeletedAsync(_context.Criteria, target.Id);
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

            await AssertNothingSoftDeletedAsync();
        }

        [Fact]
        public async Task DeleteCriterion_AlreadyDeleted_Fail()
        {
            // The !IsDeleted query filter hides soft-deleted rows, so a retried delete is
            // reported as a missing criterion and must not move the deletion timestamp
            var target = await SeedCriterionAsync(CreateAgeCriterion());
            var other = await SeedCriterionAsync(CreateHypotensionCriterion());

            var firstDelete = await _handler.HandleAsync(
                new DeleteCriterionCommand { Id = target.Id }, TestContext.Current.CancellationToken);
            Assert.True(firstDelete.IsSuccess());
            Assert.Equal(ApplicationStatus.Deleted, firstDelete.StatusCode);

            // PostgreSQL truncates the timestamp to microsecond precision, so compare
            // against the value that was actually persisted
            await using var seedContext = new ClinicalDbContext(_options);
            var storedDeletedAt = (await seedContext.Criteria.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken)).DeletedAt;

            var secondDelete = await _handler.HandleAsync(
                new DeleteCriterionCommand { Id = target.Id }, TestContext.Current.CancellationToken);

            Assert.True(secondDelete.IsFailure());
            Assert.NotNull(secondDelete.Error);
            Assert.Equal(ApplicationStatus.BadRequest, secondDelete.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);

            // The handler bails out before soft deleting again, the original timestamp stays
            var criterion = await freshContext.Criteria.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken);
            Assert.True(criterion.IsDeleted);
            Assert.Equal(storedDeletedAt, criterion.DeletedAt);

            // The unrelated criterion is untouched
            await AssertNotDeletedAsync(freshContext.Criteria, other.Id);
        }

        #endregion

        #region Read helpers

        private async Task<RiskFactor> LoadRiskFactorAsync(Guid id)
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.RiskFactors.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        }

        private async Task<MetricRule> LoadMetricRuleAsync(Guid id)
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.MetricRules.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        }

        private async Task<TreatmentProtocol> LoadTreatmentAsync(Guid id)
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.TreatmentProtocols.IgnoreQueryFilters()
                .Include(x => x.Criteria)
                .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        }

        #endregion
    }
}
