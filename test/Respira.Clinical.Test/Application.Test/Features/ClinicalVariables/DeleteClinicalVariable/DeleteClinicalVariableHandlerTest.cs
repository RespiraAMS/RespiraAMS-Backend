using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.ClinicalVariables.DeleteClinicalVariable;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Application.Test.Features.ClinicalVariables.DeleteClinicalVariable
{
    public class DeleteClinicalVariableHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly DeleteClinicalVariableHandler _handler;
        private readonly IDbContext _context;

        public DeleteClinicalVariableHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var logger = new Mock<ILogger<DeleteClinicalVariableHandler>>().Object;

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
            // because soft-deleted rows are hidden by the query filter but still occupy
            // the table
            await _context.MetricRules.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Criteria.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.ClinicalMetrics.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.ClinicalVariables.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        #region Seed helpers

        private async Task SeedAsync(params ClinicalVariable[] variables)
        {
            await _context.ClinicalVariables.AddRangeAsync(
                variables, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        private async Task<Criterion> SeedCriterionAsync(Criterion criterion)
        {
            await _context.Criteria.AddAsync(criterion, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return criterion;
        }

        private async Task<ClinicalMetric> SeedMetricAsync()
        {
            var metric = new ClinicalMetric
            {
                Name = "Severity metrics CURB-65",
                Code = "CURB-65",
                Description = "CURB-65 metrics to assess severity of a patient",
                Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                Version = "2002-2003",
                IsApplied = true,
                Rules = [],
            };
            await _context.ClinicalMetrics.AddAsync(metric, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return metric;
        }

        private async Task<ScoringRule> SeedScoringRuleAsync(
            Guid metricId, Guid criterionId, Formula scoreFunction, bool softDeleted = false)
        {
            var rule = new ScoringRule
            {
                ClinicalMetricId = metricId,
                CriterionId = criterionId,
                ScoreFunction = scoreFunction,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow.AddDays(-1) : null,
            };
            await _context.MetricRules.AddAsync(rule, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return rule;
        }

        private static Criterion CreateCriterion(
            string name, Formula formula, bool softDeleted = false)
        {
            return new Criterion
            {
                Name = name,
                Formula = formula,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow.AddDays(-1) : null,
            };
        }

        private async Task<ClinicalVariable> LoadVariableAsync(Guid id)
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        }

        private async Task<Criterion> LoadCriterionAsync(Guid id)
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.Criteria.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        }

        private async Task<int> CountSoftDeletedVariablesAsync()
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken);
        }

        private async Task<int> CountSoftDeletedCriteriaAsync()
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.Criteria.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken);
        }

        private async Task<int> CountSoftDeletedRulesAsync()
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken);
        }

        private static BooleanClinicalVariable CreateBooleanVariable(
            string code,
            string name,
            string description,
            ClinicalVariableCategory category,
            Formula? prerequisite = null)
        {
            return new BooleanClinicalVariable
            {
                Code = code,
                Name = name,
                Description = description,
                IsRequired = false,
                Category = category,
                Prerequisite = prerequisite,
            };
        }

        private static NumericClinicalVariable CreateSystolicBloodPressure()
        {
            return new NumericClinicalVariable
            {
                // LOINC 8480-6 - systolic blood pressure
                Code = "8480-6",
                Name = "Systolic blood pressure",
                Description = "Systolic blood pressure measured at the arm",
                CanonicalUnit = "mmHg",
                IsRequired = true,
                Category = ClinicalVariableCategory.Clinical,
                // Boundary: both accepted bounds are inclusive
                AcceptedRange = new Range
                {
                    Min = 0,
                    IsMinExclusive = false,
                    Max = 300,
                    IsMaxExclusive = false,
                    Unit = "mmHg",
                },
                NormalRange = new Range
                {
                    Min = 90,
                    IsMinExclusive = false,
                    Max = 140,
                    IsMaxExclusive = false,
                    Unit = "mmHg",
                },
            };
        }

        #endregion

        #region Happy path

        [Fact]
        public async Task DeleteClinicalVariable_WithoutReferences_Success()
        {
            // A variable that no prerequisite, criterion or scoring rule mentions
            var sbp = CreateSystolicBloodPressure();
            var female = CreateBooleanVariable(
                "FEMALE",
                "Female sex",
                "Whether the patient is female",
                ClinicalVariableCategory.PersonalInformation);
            await SeedAsync(sbp, female);

            var result = await _handler.HandleAsync(
                new DeleteClinicalVariableCommand { Id = sbp.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            // Business rule: the variable is soft deleted, never removed
            var deleted = await LoadVariableAsync(sbp.Id);
            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);

            // The default query filter must now hide the deleted variable
            await using var freshContext = new ClinicalDbContext(_options);
            Assert.False(await freshContext.ClinicalVariables
                .AnyAsync(x => x.Id == sbp.Id, TestContext.Current.CancellationToken));

            // The unrelated variable stays live
            var untouched = await LoadVariableAsync(female.Id);
            Assert.False(untouched.IsDeleted);
            Assert.Null(untouched.DeletedAt);
        }

        [Fact]
        public async Task DeleteClinicalVariable_OnlySoftDeletedReferences_Success()
        {
            /*
             * Soft-deleted variables, criteria and metric rules are hidden by the query
             * filter, so they are no longer part of the clinical knowledge base and must
             * not forbid the delete. Their own deletion timestamps must stay untouched
             */
            var sbp = CreateSystolicBloodPressure();
            var control = CreateBooleanVariable(
                "FEMALE", "Female sex", "Whether the patient is female",
                ClinicalVariableCategory.PersonalInformation);

            var deletedReference = CreateBooleanVariable(
                "HYPOTENSION",
                "Hypotension",
                "Whether the systolic blood pressure is below the hypotension threshold",
                ClinicalVariableCategory.Clinical,
                new BinaryFormula(
                    new VariableFormula(sbp),
                    new NumericConstantFormula(90),
                    ExpressionOperator.Lt));
            deletedReference.IsDeleted = true;
            deletedReference.DeletedAt = DateTimeOffset.UtcNow.AddDays(-1);

            var deletedCriterion = CreateCriterion(
                "Huyết áp tâm thu < 90 mmHg",
                new BinaryFormula(
                    new VariableFormula(sbp),
                    new NumericConstantFormula(90),
                    ExpressionOperator.Lt),
                softDeleted: true);

            var metric = await SeedMetricAsync();
            var liveCriterion = await SeedCriterionAsync(
                CreateCriterion("Always true", new BooleanConstantFormula(true)));
            await SeedScoringRuleAsync(
                metric.Id,
                liveCriterion.Id,
                new TernaryFormula(
                    new BinaryFormula(
                        new VariableFormula(sbp),
                        new NumericConstantFormula(90),
                        ExpressionOperator.Lt),
                    new NumericConstantFormula(1),
                    new NumericConstantFormula(0)),
                softDeleted: true);

            await SeedAsync(sbp, control, deletedReference);
            await SeedCriterionAsync(deletedCriterion);

            var referenceDeletedAt = (await LoadVariableAsync(deletedReference.Id)).DeletedAt;
            var criterionDeletedAt = (await LoadCriterionAsync(deletedCriterion.Id)).DeletedAt;

            var result = await _handler.HandleAsync(
                new DeleteClinicalVariableCommand { Id = sbp.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);
            Assert.True((await LoadVariableAsync(sbp.Id)).IsDeleted);

            var keptReference = await LoadVariableAsync(deletedReference.Id);
            Assert.True(keptReference.IsDeleted);
            Assert.Equal(referenceDeletedAt, keptReference.DeletedAt);
            Assert.NotNull(keptReference.Prerequisite);

            var keptCriterion = await LoadCriterionAsync(deletedCriterion.Id);
            Assert.True(keptCriterion.IsDeleted);
            Assert.Equal(criterionDeletedAt, keptCriterion.DeletedAt);

            // The metric rule is untouched as well
            Assert.Equal(1, await CountSoftDeletedRulesAsync());
            Assert.False((await LoadVariableAsync(control.Id)).IsDeleted);
            Assert.False((await LoadCriterionAsync(liveCriterion.Id)).IsDeleted);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task DeleteClinicalVariable_ReferencedByPrerequisite_Fail()
        {
            // Business rule: another variable's prerequisite formula uses this variable
            var female = CreateBooleanVariable(
                "FEMALE", "Female sex", "Whether the patient is female",
                ClinicalVariableCategory.PersonalInformation);
            var pregnantOrLactating = CreateBooleanVariable(
                "PREGNANT-OR-LACTATING",
                "Pregnant or lactating",
                "Whether the patient is currently pregnant or lactating",
                ClinicalVariableCategory.PersonalInformation,
                new VariableFormula(female));
            await SeedAsync(female, pregnantOrLactating);

            var result = await _handler.HandleAsync(
                new DeleteClinicalVariableCommand { Id = female.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);

            // Nothing may be soft deleted and the prerequisite must stay untouched
            Assert.Equal(0, await CountSoftDeletedVariablesAsync());
            var kept = await LoadVariableAsync(pregnantOrLactating.Id);
            Assert.False(kept.IsDeleted);
            Assert.NotNull(kept.Prerequisite);
            Assert.False((await LoadVariableAsync(female.Id)).IsDeleted);
        }

        [Fact]
        public async Task DeleteClinicalVariable_ReferencedByNestedPrerequisite_Fail()
        {
            /*
             * The reference can sit at any depth of the prerequisite formula:
             * PREGNANCY-INDUCED-HYPERTENSION = FEMALE AND (SBP > 140)
             */
            var sbp = CreateSystolicBloodPressure();
            var female = CreateBooleanVariable(
                "FEMALE", "Female sex", "Whether the patient is female",
                ClinicalVariableCategory.PersonalInformation);
            var pih = CreateBooleanVariable(
                "PREGNANCY-INDUCED-HYPERTENSION",
                "Pregnancy induced hypertension",
                "Whether the patient has pregnancy induced hypertension",
                ClinicalVariableCategory.Clinical,
                new BinaryFormula(
                    new VariableFormula(female),
                    new BinaryFormula(
                        new VariableFormula(sbp),
                        new NumericConstantFormula(140),
                        ExpressionOperator.Gt),
                    ExpressionOperator.And));
            await SeedAsync(sbp, female, pih);

            var result = await _handler.HandleAsync(
                new DeleteClinicalVariableCommand { Id = sbp.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);

            Assert.Equal(0, await CountSoftDeletedVariablesAsync());
            var kept = await LoadVariableAsync(pih.Id);
            Assert.False(kept.IsDeleted);
            Assert.NotNull(kept.Prerequisite);
            Assert.False((await LoadVariableAsync(female.Id)).IsDeleted);
        }

        [Fact]
        public async Task DeleteClinicalVariable_ReferencedByCriterion_Fail()
        {
            // Business rule: a criterion formula uses this variable
            var sbp = CreateSystolicBloodPressure();
            await SeedAsync(sbp);
            var criterion = await SeedCriterionAsync(CreateCriterion(
                "Huyết áp tâm thu < 90 mmHg",
                new BinaryFormula(
                    new VariableFormula(sbp),
                    new NumericConstantFormula(90),
                    ExpressionOperator.Lt)));

            var result = await _handler.HandleAsync(
                new DeleteClinicalVariableCommand { Id = sbp.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);

            Assert.Equal(0, await CountSoftDeletedVariablesAsync());
            Assert.Equal(0, await CountSoftDeletedCriteriaAsync());
            Assert.False((await LoadCriterionAsync(criterion.Id)).IsDeleted);
            Assert.False((await LoadVariableAsync(sbp.Id)).IsDeleted);
        }

        [Fact]
        public async Task DeleteClinicalVariable_ReferencedByScoringRule_Fail()
        {
            /*
             * Business rule: the score function of a scoring rule uses this variable.
             * The rule criterion is a constant on purpose, so only the score function can
             * be the reason of the rejection (CURB-65 hypotension is worth 1 point)
             */
            var sbp = CreateSystolicBloodPressure();
            var metric = await SeedMetricAsync();
            var criterion = await SeedCriterionAsync(CreateCriterion(
                "Always true", new BooleanConstantFormula(true)));
            var rule = await SeedScoringRuleAsync(
                metric.Id,
                criterion.Id,
                new TernaryFormula(
                    new BinaryFormula(
                        new VariableFormula(sbp),
                        new NumericConstantFormula(90),
                        ExpressionOperator.Lt),
                    new NumericConstantFormula(1),
                    new NumericConstantFormula(0)));
            await SeedAsync(sbp);

            var result = await _handler.HandleAsync(
                new DeleteClinicalVariableCommand { Id = sbp.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);

            Assert.Equal(0, await CountSoftDeletedVariablesAsync());
            Assert.Equal(0, await CountSoftDeletedRulesAsync());
            Assert.False((await LoadVariableAsync(sbp.Id)).IsDeleted);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.MetricRules.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken));
            Assert.True(await freshContext.MetricRules.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == rule.Id, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteClinicalVariable_MultipleReferences_Fail()
        {
            // Upper boundary of the rule: both a prerequisite and a criterion use it
            var sbp = CreateSystolicBloodPressure();
            var female = CreateBooleanVariable(
                "FEMALE", "Female sex", "Whether the patient is female",
                ClinicalVariableCategory.PersonalInformation,
                new BinaryFormula(
                    new VariableFormula(sbp),
                    new NumericConstantFormula(90),
                    ExpressionOperator.Lt));
            await SeedAsync(sbp, female);
            var criterion = await SeedCriterionAsync(CreateCriterion(
                "Huyết áp tâm thu < 90 mmHg",
                new BinaryFormula(
                    new VariableFormula(sbp),
                    new NumericConstantFormula(90),
                    ExpressionOperator.Lt)));

            var result = await _handler.HandleAsync(
                new DeleteClinicalVariableCommand { Id = sbp.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);

            Assert.Equal(0, await CountSoftDeletedVariablesAsync());
            Assert.Equal(0, await CountSoftDeletedCriteriaAsync());
            var keptVariable = await LoadVariableAsync(female.Id);
            Assert.NotNull(keptVariable.Prerequisite);
            Assert.False((await LoadCriterionAsync(criterion.Id)).IsDeleted);
        }

        [Fact]
        public async Task DeleteClinicalVariable_UnknownId_Fail()
        {
            var sbp = CreateSystolicBloodPressure();
            await SeedAsync(sbp);
            var unknownId = Guid.CreateVersion7();

            var result = await _handler.HandleAsync(
                new DeleteClinicalVariableCommand { Id = unknownId },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Nothing must be soft-deleted when the target does not exist
            Assert.Equal(0, await CountSoftDeletedVariablesAsync());
        }

        [Fact]
        public async Task DeleteClinicalVariable_EmptyId_Fail()
        {
            // Boundary: Guid.Empty can never match a stored variable
            await SeedAsync(CreateSystolicBloodPressure());

            var result = await _handler.HandleAsync(
                new DeleteClinicalVariableCommand { Id = Guid.Empty },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            Assert.Equal(0, await CountSoftDeletedVariablesAsync());
        }

        [Fact]
        public async Task DeleteClinicalVariable_AlreadyDeleted_Fail()
        {
            // The !IsDeleted query filter hides soft-deleted rows, so a retried delete is
            // reported as a missing variable and must not move the deletion timestamp
            var female = CreateBooleanVariable(
                "FEMALE", "Female sex", "Whether the patient is female",
                ClinicalVariableCategory.PersonalInformation);
            female.IsDeleted = true;
            female.DeletedAt = DateTimeOffset.UtcNow.AddDays(-1);
            var sbp = CreateSystolicBloodPressure();
            await SeedAsync(female, sbp);

            var originalDeletedAt = (await LoadVariableAsync(female.Id)).DeletedAt;

            var result = await _handler.HandleAsync(
                new DeleteClinicalVariableCommand { Id = female.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            var unchanged = await LoadVariableAsync(female.Id);
            Assert.True(unchanged.IsDeleted);
            Assert.Equal(originalDeletedAt, unchanged.DeletedAt);
            Assert.False((await LoadVariableAsync(sbp.Id)).IsDeleted);
        }

        #endregion
    }
}
