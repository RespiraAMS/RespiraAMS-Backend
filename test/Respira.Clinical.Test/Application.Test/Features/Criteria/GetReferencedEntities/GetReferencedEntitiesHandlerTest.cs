using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.Criteria.GetReferencedEntities;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.Criteria.GetReferencedEntities
{
    public class GetReferencedEntitiesHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly GetReferencedEntitiesHandler _handler;
        private readonly IDbContext _context;

        public GetReferencedEntitiesHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var logger = new Mock<ILogger<GetReferencedEntitiesHandler>>().Object;

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
            // id assertions are deterministic across runs. IgnoreQueryFilters because
            // soft deleted rows are hidden by the query filter but still occupy the table
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
         * The two criteria every scenario needs: the one being inspected and an
         * unrelated control whose references must never be reported. The formulas
         * mirror the real seed data (CURB-65 age >= 65, hypotension SBP < 90 mmHg)
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

        private async Task<RiskFactor> SeedRiskFactorAsync(
            Guid pathogenId, Guid criterionId, bool isDeleted = false)
        {
            var factor = new RiskFactor
            {
                PathogenId = pathogenId,
                CriterionId = criterionId,
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.RiskFactors.AddAsync(factor, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return factor;
        }

        // Every CURB-65 criterion contributes exactly 1 point, so the score function is
        // a constant 1 rather than an arbitrary number
        private async Task<MetricsRule> SeedScoringRuleAsync(
            Guid metricsId, Guid criterionId, bool isDeleted = false)
        {
            var rule = new ScoringRule
            {
                ClinicalMetricsId = metricsId,
                CriterionId = criterionId,
                ScoreFunction = new NumericConstantFormula(1),
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.MetricsRules.AddAsync(rule, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return rule;
        }

        private async Task<Treatment> SeedTreatmentAsync(
            Severity severity,
            TreatmentSite treatmentSite,
            IEnumerable<Criterion> criteria,
            bool isDeleted = false)
        {
            var treatment = new Treatment
            {
                Severity = severity,
                TreatmentSite = treatmentSite,
                Criteria = [.. criteria],
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.Treatments.AddAsync(treatment, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return treatment;
        }

        private static GetReferencedEntitiesQuery Query(Guid id) => new() { Id = id };

        // The handler returns the ids without any ordering contract, so the assertions
        // compare them as sets instead of relying on a physical row order
        private static Guid[] Sorted(IEnumerable<Guid> ids) => [.. ids.Order()];

        #region Happy path

        [Fact]
        public async Task GetReferencedEntities_NoReferences_ReturnsEmptyAndFlagFalse_Success()
        {
            // Lower boundary: 0 references, so the criterion is safe to change freely
            var criterion = await SeedCriterionAsync(CreateAgeCriterion());

            var result = await _handler.HandleAsync(Query(criterion.Id),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.False(result.Data.HasReferencedEntities);
            Assert.Empty(result.Data.RiskFactorReference);
            Assert.Empty(result.Data.MetricsRuleReference);
            Assert.Empty(result.Data.TreatmentReference);
        }

        [Fact]
        public async Task GetReferencedEntities_SingleRiskFactor_ReportsItsId_Success()
        {
            // Smallest non zero boundary: exactly one referencing entity flips the flag
            var criterion = await SeedCriterionAsync(CreateAgeCriterion());
            var pathogen = await SeedPathogenAsync(
                "Klebsiella pneumoniae",
                "Gram-negative bacillus causing hospital-acquired pneumonia",
                isAtypical: true);
            var factor = await SeedRiskFactorAsync(pathogen.Id, criterion.Id);

            var result = await _handler.HandleAsync(Query(criterion.Id),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.True(result.Data.HasReferencedEntities);
            Assert.Equal([factor.Id], Sorted(result.Data.RiskFactorReference));
            Assert.Empty(result.Data.MetricsRuleReference);
            Assert.Empty(result.Data.TreatmentReference);
        }

        [Fact]
        public async Task GetReferencedEntities_SingleMetricsRule_ReportsItsId_Success()
        {
            var criterion = await SeedCriterionAsync(CreateHypotensionCriterion());
            var metrics = await SeedMetricsAsync(
                "Severity metrics CURB-65",
                "CURB-65",
                "CURB-65 metrics to assess severity of a patient");
            var rule = await SeedScoringRuleAsync(metrics.Id, criterion.Id);

            var result = await _handler.HandleAsync(Query(criterion.Id),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.True(result.Data.HasReferencedEntities);
            Assert.Empty(result.Data.RiskFactorReference);
            Assert.Equal([rule.Id], Sorted(result.Data.MetricsRuleReference));
            Assert.Empty(result.Data.TreatmentReference);
        }

        [Fact]
        public async Task GetReferencedEntities_SingleTreatment_ReportsItsId_Success()
        {
            var criterion = await SeedCriterionAsync(CreateHypotensionCriterion());
            var treatment = await SeedTreatmentAsync(
                Severity.Severe, TreatmentSite.IntensiveCareUnit, [criterion]);

            var result = await _handler.HandleAsync(Query(criterion.Id),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.True(result.Data.HasReferencedEntities);
            Assert.Empty(result.Data.RiskFactorReference);
            Assert.Empty(result.Data.MetricsRuleReference);
            Assert.Equal([treatment.Id], Sorted(result.Data.TreatmentReference));
        }

        [Fact]
        public async Task GetReferencedEntities_EveryReferenceKind_ReportedTogether_Success()
        {
            // Upper boundary: several referencing entities of every kind at once
            var criterion = await SeedCriterionAsync(CreateAgeCriterion());
            var pathogen = await SeedPathogenAsync(
                "Streptococcus pneumoniae",
                "Gram-positive diplococcus, the most common community acquired pathogen",
                isAtypical: false);
            var metrics = await SeedMetricsAsync(
                "Severity metrics CURB-65",
                "CURB-65",
                "CURB-65 metrics to assess severity of a patient");

            var factorOne = await SeedRiskFactorAsync(pathogen.Id, criterion.Id);
            var factorTwo = await SeedRiskFactorAsync(pathogen.Id, criterion.Id);
            var ruleOne = await SeedScoringRuleAsync(metrics.Id, criterion.Id);
            var ruleTwo = await SeedScoringRuleAsync(metrics.Id, criterion.Id);
            var treatmentOne = await SeedTreatmentAsync(
                Severity.Moderate, TreatmentSite.Inpatient, [criterion]);
            var treatmentTwo = await SeedTreatmentAsync(
                Severity.Severe, TreatmentSite.IntensiveCareUnit, [criterion]);

            var result = await _handler.HandleAsync(Query(criterion.Id),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.True(result.Data.HasReferencedEntities);
            Assert.Equal(Sorted([factorOne.Id, factorTwo.Id]), Sorted(result.Data.RiskFactorReference));
            Assert.Equal(Sorted([ruleOne.Id, ruleTwo.Id]), Sorted(result.Data.MetricsRuleReference));
            Assert.Equal(Sorted([treatmentOne.Id, treatmentTwo.Id]), Sorted(result.Data.TreatmentReference));
        }

        [Fact]
        public async Task GetReferencedEntities_OnlyTargetCriterionReferences_AreReported_Success()
        {
            /*
             * Business rule: the endpoint answers "what breaks if this criterion
             * changes", so references of an unrelated criterion must stay out. A
             * treatment may legitimately hold several criteria, and it is reported
             * because the inspected criterion is one of them
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

            var targetFactor = await SeedRiskFactorAsync(pathogen.Id, target.Id);
            var otherFactor = await SeedRiskFactorAsync(pathogen.Id, other.Id);
            var targetRule = await SeedScoringRuleAsync(metrics.Id, target.Id);
            var otherRule = await SeedScoringRuleAsync(metrics.Id, other.Id);
            var sharedTreatment = await SeedTreatmentAsync(
                Severity.Moderate, TreatmentSite.Inpatient, [target, other]);

            var result = await _handler.HandleAsync(Query(target.Id),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.True(result.Data.HasReferencedEntities);
            Assert.Equal([targetFactor.Id], Sorted(result.Data.RiskFactorReference));
            Assert.Equal([targetRule.Id], Sorted(result.Data.MetricsRuleReference));
            Assert.Equal([sharedTreatment.Id], Sorted(result.Data.TreatmentReference));
            Assert.DoesNotContain(otherFactor.Id, result.Data.RiskFactorReference);
            Assert.DoesNotContain(otherRule.Id, result.Data.MetricsRuleReference);
        }

        [Fact]
        public async Task GetReferencedEntities_SoftDeletedReferences_AreNotReported_Success()
        {
            // A soft deleted entity is not affected by a criterion change any more,
            // so only the live references may be reported
            var criterion = await SeedCriterionAsync(CreateAgeCriterion());
            var pathogen = await SeedPathogenAsync(
                "Klebsiella pneumoniae",
                "Gram-negative bacillus causing hospital-acquired pneumonia",
                isAtypical: true);
            var metrics = await SeedMetricsAsync(
                "Severity metrics CURB-65",
                "CURB-65",
                "CURB-65 metrics to assess severity of a patient");

            var liveFactor = await SeedRiskFactorAsync(pathogen.Id, criterion.Id);
            await SeedRiskFactorAsync(pathogen.Id, criterion.Id, isDeleted: true);
            var liveRule = await SeedScoringRuleAsync(metrics.Id, criterion.Id);
            await SeedScoringRuleAsync(metrics.Id, criterion.Id, isDeleted: true);
            var liveTreatment = await SeedTreatmentAsync(
                Severity.Moderate, TreatmentSite.Inpatient, [criterion]);
            await SeedTreatmentAsync(
                Severity.Mild, TreatmentSite.Outpatient, [criterion], isDeleted: true);

            var result = await _handler.HandleAsync(Query(criterion.Id),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.True(result.Data.HasReferencedEntities);
            Assert.Equal([liveFactor.Id], Sorted(result.Data.RiskFactorReference));
            Assert.Equal([liveRule.Id], Sorted(result.Data.MetricsRuleReference));
            Assert.Equal([liveTreatment.Id], Sorted(result.Data.TreatmentReference));
        }

        [Fact]
        public async Task GetReferencedEntities_AllReferencesSoftDeleted_FlagIsFalse_Success()
        {
            // Upper boundary of the flag: the only referencing rows are all soft
            // deleted, so nothing is left that a criterion change could affect
            var criterion = await SeedCriterionAsync(CreateAgeCriterion());
            var pathogen = await SeedPathogenAsync(
                "Klebsiella pneumoniae",
                "Gram-negative bacillus causing hospital-acquired pneumonia",
                isAtypical: true);

            await SeedRiskFactorAsync(pathogen.Id, criterion.Id, isDeleted: true);

            var result = await _handler.HandleAsync(Query(criterion.Id),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.False(result.Data.HasReferencedEntities);
            Assert.Empty(result.Data.RiskFactorReference);
            Assert.Empty(result.Data.MetricsRuleReference);
            Assert.Empty(result.Data.TreatmentReference);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task GetReferencedEntities_UnknownId_Fail()
        {
            var result = await _handler.HandleAsync(Query(Guid.CreateVersion7()),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetReferencedEntities_EmptyId_Fail()
        {
            var result = await _handler.HandleAsync(Query(Guid.Empty),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetReferencedEntities_SoftDeletedCriterion_Fail()
        {
            // A soft deleted criterion is hidden by the query filter, so it behaves
            // exactly like an unknown id instead of reporting stale references
            var criterion = CreateAgeCriterion();
            criterion.IsDeleted = true;
            criterion.DeletedAt = DateTimeOffset.UtcNow;
            await SeedCriterionAsync(criterion);

            var result = await _handler.HandleAsync(Query(criterion.Id),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        #endregion
    }
}
