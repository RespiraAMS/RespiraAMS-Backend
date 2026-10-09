using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.Criteria.UpdateCriterion;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.ServiceDefaults.Models;
using FormulaDto = Respira.Clinical.Application.Features.Shared.ManageFormula.FormulaDto;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Application.Test.Features.Criteria.UpdateCriterion
{
    public class UpdateCriterionHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly UpdateCriterionHandler _handler;
        private readonly IDbContext _context;

        public UpdateCriterionHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var mapper = new UpdateCriterionMapper(new FormulaMapper());
            var logger = new Mock<ILogger<UpdateCriterionHandler>>().Object;

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
            await _context.ClinicalVariables.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        private sealed record SeedData(
            Criterion Target,
            Criterion Control,
            ClinicalVariable Age,
            ClinicalVariable Sbp,
            ClinicalVariable Female,
            Pathogen Pathogen,
            RiskFactor TargetFactor,
            RiskFactor ControlFactor,
            ClinicalMetrics Metrics,
            MetricsRule TargetRule,
            MetricsRule ControlRule,
            Treatment TargetTreatment,
            Treatment ControlTreatment,
            DateTimeOffset StoredTargetUpdatedAt,
            DateTimeOffset StoredControlUpdatedAt);

        /*
         * One full scenario: the clinical variables the formulas reference, the target
         * criterion (Tuổi >= 65) and an unrelated control criterion (Huyết áp tâm thu
         * < 90 mmHg), plus a risk factor, a CURB-65 scoring rule and a treatment
         * protocol attached to each of them
         */
        private async Task<SeedData> SeedAsync()
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
            };
            var sbp = new NumericClinicalVariable
            {
                Code = "SBP",
                Name = "Huyết áp tâm thu",
                Description = "Systolic blood pressure measured at the arm",
                CanonicalUnit = "mmHg",
                IsRequired = true,
                Category = ClinicalVariableCategory.Clinical,
                AcceptedRange = CreateRange(0, 300, "mmHg"),
                NormalRange = CreateRange(90, 140, "mmHg"),
            };
            var female = new BooleanClinicalVariable
            {
                Code = "FEMALE",
                Name = "Giới tính nữ",
                Description = "Whether the patient is female",
                IsRequired = false,
                Category = ClinicalVariableCategory.PersonalInformation,
            };
            await _context.ClinicalVariables.AddRangeAsync(
                [age, sbp, female], TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var target = new Criterion(
                "Tuổi >= 65",
                new BinaryFormula(
                    new VariableFormula(age), new NumericConstantFormula(65), ExpressionOperator.GTE));
            var control = new Criterion(
                "Huyết áp tâm thu < 90 mmHg",
                new BinaryFormula(
                    new VariableFormula(sbp), new NumericConstantFormula(90), ExpressionOperator.LT));
            await _context.Criteria.AddRangeAsync(
                [target, control], TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var pathogen = new Pathogen
            {
                Name = "Klebsiella pneumoniae",
                Description = "Gram-negative bacillus causing hospital-acquired pneumonia",
                IsAtypical = true,
            };
            await _context.Pathogens.AddAsync(pathogen, TestContext.Current.CancellationToken);
            var metrics = new ClinicalMetrics
            {
                Name = "Severity metrics CURB-65",
                Code = "CURB-65",
                Description = "CURB-65 metrics to assess severity of a patient",
            };
            await _context.ClinicalMetrics.AddAsync(metrics, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var targetFactor = new RiskFactor { PathogenId = pathogen.Id, CriterionId = target.Id };
            var controlFactor = new RiskFactor { PathogenId = pathogen.Id, CriterionId = control.Id };
            await _context.RiskFactors.AddRangeAsync(
                [targetFactor, controlFactor], TestContext.Current.CancellationToken);

            var targetRule = new ScoringRule
            {
                ClinicalMetricsId = metrics.Id,
                CriterionId = target.Id,
                // Every CURB-65 criterion contributes exactly 1 point
                ScoreFunction = new NumericConstantFormula(1),
            };
            var controlRule = new ScoringRule
            {
                ClinicalMetricsId = metrics.Id,
                CriterionId = control.Id,
                ScoreFunction = new NumericConstantFormula(1),
            };
            await _context.MetricsRules.AddRangeAsync(
                [targetRule, controlRule], TestContext.Current.CancellationToken);

            var targetTreatment = new Treatment
            {
                Severity = Severity.Severe,
                TreatmentSite = TreatmentSite.IntensiveCareUnit,
                Criteria = [target],
            };
            var controlTreatment = new Treatment
            {
                Severity = Severity.Mild,
                TreatmentSite = TreatmentSite.Outpatient,
                Criteria = [control],
            };
            await _context.Treatments.AddRangeAsync(
                [targetTreatment, controlTreatment], TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            // PostgreSQL truncates timestamps to microsecond precision, so the persisted
            // value is what every comparison below has to use
            await using var freshContext = new ClinicalDbContext(_options);
            var storedCriteria = await freshContext.Criteria
                .Where(x => x.Id == target.Id || x.Id == control.Id)
                .ToListAsync(TestContext.Current.CancellationToken);

            return new SeedData(
                target, control, age, sbp, female,
                pathogen, targetFactor, controlFactor,
                metrics, targetRule, controlRule,
                targetTreatment, controlTreatment,
                storedCriteria.Single(x => x.Id == target.Id).UpdatedAt,
                storedCriteria.Single(x => x.Id == control.Id).UpdatedAt);
        }

        private static Range CreateRange(decimal min, decimal max, string unit)
        {
            return new Range
            {
                Min = min,
                IsMinExclusive = false,
                Max = max,
                IsMaxExclusive = false,
                Unit = unit,
            };
        }

        private static FormulaDto Variable(Guid id) => new() { Variable = id };
        private static FormulaDto Constant(string value) => new() { Constant = value };
        private static FormulaDto Binary(FormulaDto left, ExpressionOperator op, FormulaDto right) =>
            new() { Left = left, Right = right, Operator = op };

        private async Task<Criterion> ReadCriterionAsync(Guid id)
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.Criteria.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        }

        private async Task AssertUnchangedAsync(
            Guid id, string expectedName, string expectedFormula, DateTimeOffset expectedUpdatedAt)
        {
            var criterion = await ReadCriterionAsync(id);
            Assert.Equal(expectedName, criterion.Name);
            Assert.Equal(expectedFormula, criterion.Formula.ToString());
            Assert.Equal(expectedUpdatedAt, criterion.UpdatedAt);
            Assert.False(criterion.IsDeleted);
            Assert.Null(criterion.DeletedAt);
        }

        #region Happy path

        [Fact]
        public async Task UpdateCriterion_RenameAndChangeFormula_Success()
        {
            var data = await SeedAsync();
            var command = new UpdateCriterionCommand
            {
                Id = data.Target.Id,
                Name = "Tuổi > 65",
                Formula = Binary(Variable(data.Age.Id), ExpressionOperator.GT, Constant("65")),
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            var saved = await ReadCriterionAsync(data.Target.Id);
            Assert.Equal("Tuổi > 65", saved.Name);
            Assert.False(saved.IsDeleted);

            var formula = Assert.IsType<BinaryFormula>(saved.Formula);
            Assert.Equal(ExpressionOperator.GT, formula.Operator);
            Assert.Equal(65m, Assert.IsType<NumericConstantFormula>(formula.Right).Constant);
            Assert.Equal("AGE > 65", formula.ToString());
            Assert.Equal(ExpressionResultType.Boolean, formula.ResultType);

            // The update moves the timestamp forward and rewrites nothing else
            Assert.True(saved.UpdatedAt > data.StoredTargetUpdatedAt);
            Assert.Equal(data.Target.Id, saved.Id);

            // The unrelated criterion keeps its payload
            var control = await ReadCriterionAsync(data.Control.Id);
            Assert.Equal("Huyết áp tâm thu < 90 mmHg", control.Name);
            Assert.Equal("SBP < 90", control.Formula.ToString());
            Assert.False(control.IsDeleted);
        }

        [Fact]
        public async Task UpdateCriterion_BooleanConstantFormula_Success()
        {
            var data = await SeedAsync();
            var command = new UpdateCriterionCommand
            {
                Id = data.Target.Id,
                Name = "Universal triage screening",
                Formula = Constant("true"),
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            var saved = await ReadCriterionAsync(data.Target.Id);
            Assert.Equal("Universal triage screening", saved.Name);
            var formula = Assert.IsType<BooleanConstantFormula>(saved.Formula);
            Assert.True(formula.Constant);
            Assert.Empty(saved.Variables);
        }

        /*
         * Boundary values of the systolic blood pressure accepted range (0-300 mmHg)
         * and its normal range (90-140 mmHg). 90 mmHg is the hypotension threshold and
         * 140 mmHg the hypertension threshold, so both are clinical decision points and
         * not arbitrary numbers
         */
        [Theory]
        [InlineData("0")]
        [InlineData("90")]
        [InlineData("140")]
        [InlineData("300")]
        public async Task UpdateCriterion_NumericBoundaryFormula_Success(string threshold)
        {
            var data = await SeedAsync();
            var command = new UpdateCriterionCommand
            {
                Id = data.Target.Id,
                Name = "Hypotension threshold",
                Formula = Binary(Variable(data.Sbp.Id), ExpressionOperator.LT, Constant(threshold)),
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            var saved = await ReadCriterionAsync(data.Target.Id);
            var formula = Assert.IsType<BinaryFormula>(saved.Formula);
            Assert.Equal(ExpressionOperator.LT, formula.Operator);
            Assert.Equal(
                decimal.Parse(threshold, CultureInfo.InvariantCulture),
                Assert.IsType<NumericConstantFormula>(formula.Right).Constant);
            Assert.Equal($"SBP < {threshold}", formula.ToString());

            var variable = Assert.Single(saved.Variables);
            Assert.Equal("SBP", variable.Code);
            Assert.Equal(ClinicalValueType.Numeric, variable.ValueType);
        }

        [Fact]
        public async Task UpdateCriterion_NestedLogicalFormula_Success()
        {
            var data = await SeedAsync();
            var command = new UpdateCriterionCommand
            {
                Id = data.Target.Id,
                Name = "Female hypertensive crisis",
                Formula = Binary(
                    Variable(data.Female.Id),
                    ExpressionOperator.AND,
                    Binary(Variable(data.Sbp.Id), ExpressionOperator.GT, Constant("140"))),
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            var saved = await ReadCriterionAsync(data.Target.Id);
            var formula = Assert.IsType<BinaryFormula>(saved.Formula);
            Assert.Equal(ExpressionOperator.AND, formula.Operator);
            Assert.Equal("FEMALE AND (SBP > 140)", formula.ToString());

            Assert.Equal(2, saved.Variables.Count());
            Assert.Contains(saved.Variables, x => x.Code == "FEMALE");
            Assert.Contains(saved.Variables, x => x.Code == "SBP");
        }

        [Fact]
        public async Task UpdateCriterion_AssociatedRowsNotCascaded_Success()
        {
            /*
             * Business rule: unlike a delete, an update only rewrites the criterion
             * payload. The risk factor, the scoring rule and the treatment protocol
             * that reference it keep living untouched
             */
            var data = await SeedAsync();
            var command = new UpdateCriterionCommand
            {
                Id = data.Target.Id,
                Name = "Tuổi > 65",
                Formula = Binary(Variable(data.Age.Id), ExpressionOperator.GT, Constant("65")),
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var readContext = new ClinicalDbContext(_options);

            Assert.False(await readContext.RiskFactors.IgnoreQueryFilters()
                .AnyAsync(x => x.IsDeleted, TestContext.Current.CancellationToken));
            Assert.False(await readContext.MetricsRules.IgnoreQueryFilters()
                .AnyAsync(x => x.IsDeleted, TestContext.Current.CancellationToken));
            Assert.False(await readContext.Treatments.IgnoreQueryFilters()
                .AnyAsync(x => x.IsDeleted, TestContext.Current.CancellationToken));

            var targetFactor = await readContext.RiskFactors.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == data.TargetFactor.Id, TestContext.Current.CancellationToken);
            Assert.False(targetFactor.IsDeleted);
            Assert.Null(targetFactor.DeletedAt);

            var targetRule = await readContext.MetricsRules.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == data.TargetRule.Id, TestContext.Current.CancellationToken);
            Assert.False(targetRule.IsDeleted);
            Assert.Null(targetRule.DeletedAt);

            var targetTreatment = await readContext.Treatments.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == data.TargetTreatment.Id, TestContext.Current.CancellationToken);
            Assert.False(targetTreatment.IsDeleted);
            Assert.Null(targetTreatment.DeletedAt);

            // The parents of the cascade are untouched as well
            await AssertNotDeletedAsync(readContext.Pathogens, data.Pathogen.Id);
            await AssertNotDeletedAsync(readContext.ClinicalMetrics, data.Metrics.Id);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task UpdateCriterion_UnknownId_Fail()
        {
            var data = await SeedAsync();
            var unknownId = Guid.CreateVersion7();

            var result = await _handler.HandleAsync(
                new UpdateCriterionCommand
                {
                    Id = unknownId,
                    Name = "Tuổi > 65",
                    Formula = Constant("true"),
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Nothing changed on either criterion
            await AssertUnchangedAsync(
                data.Target.Id,
                "Tuổi >= 65",
                "AGE ≥ 65",
                data.StoredTargetUpdatedAt);
            await AssertUnchangedAsync(
                data.Control.Id,
                "Huyết áp tâm thu < 90 mmHg",
                "SBP < 90",
                data.StoredControlUpdatedAt);
        }

        [Fact]
        public async Task UpdateCriterion_EmptyId_Fail()
        {
            // Boundary: Guid.Empty can never match a stored criterion
            var data = await SeedAsync();

            var result = await _handler.HandleAsync(
                new UpdateCriterionCommand
                {
                    Id = Guid.Empty,
                    Name = "Tuổi > 65",
                    Formula = Constant("true"),
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await AssertUnchangedAsync(
                data.Target.Id,
                "Tuổi >= 65",
                "AGE ≥ 65",
                data.StoredTargetUpdatedAt);
        }

        [Fact]
        public async Task UpdateCriterion_SoftDeletedCriterion_Fail()
        {
            // The !IsDeleted query filter hides soft-deleted rows, so updating one
            // is reported as a missing criterion
            var data = await SeedAsync();
            var target = await _context.Criteria
                .SingleAsync(x => x.Id == data.Target.Id, TestContext.Current.CancellationToken);
            target.IsDeleted = true;
            target.DeletedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(
                new UpdateCriterionCommand
                {
                    Id = data.Target.Id,
                    Name = "Tuổi > 65",
                    Formula = Constant("true"),
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            var saved = await ReadCriterionAsync(data.Target.Id);
            Assert.Equal("Tuổi >= 65", saved.Name);
            Assert.Equal("AGE ≥ 65", saved.Formula.ToString());
            Assert.True(saved.IsDeleted);
            Assert.NotNull(saved.DeletedAt);
        }

        [Fact]
        public async Task UpdateCriterion_NonBooleanFormula_Fail()
        {
            // Business rule: a criterion formula must evaluate to a boolean value
            var data = await SeedAsync();

            var result = await _handler.HandleAsync(
                new UpdateCriterionCommand
                {
                    Id = data.Target.Id,
                    Name = "Age in years",
                    Formula = Binary(
                        Variable(data.Age.Id), ExpressionOperator.ADD, Constant("65")),
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await AssertUnchangedAsync(
                data.Target.Id,
                "Tuổi >= 65",
                "AGE ≥ 65",
                data.StoredTargetUpdatedAt);
        }

        [Fact]
        public async Task UpdateCriterion_UnknownVariable_Fail()
        {
            var data = await SeedAsync();

            var result = await _handler.HandleAsync(
                new UpdateCriterionCommand
                {
                    Id = data.Target.Id,
                    Name = "Female-specific risk",
                    Formula = Variable(Guid.CreateVersion7()),
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await AssertUnchangedAsync(
                data.Target.Id,
                "Tuổi >= 65",
                "AGE ≥ 65",
                data.StoredTargetUpdatedAt);
        }

        [Fact]
        public async Task UpdateCriterion_SoftDeletedVariable_Fail()
        {
            // A soft-deleted variable is hidden by the query filter, so the formula
            // can no longer be resolved
            var data = await SeedAsync();
            var age = await _context.ClinicalVariables
                .SingleAsync(x => x.Id == data.Age.Id, TestContext.Current.CancellationToken);
            age.IsDeleted = true;
            age.DeletedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(
                new UpdateCriterionCommand
                {
                    Id = data.Target.Id,
                    Name = "Tuổi > 65",
                    Formula = Binary(Variable(data.Age.Id), ExpressionOperator.GT, Constant("65")),
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await AssertUnchangedAsync(
                data.Target.Id,
                "Tuổi >= 65",
                "AGE ≥ 65",
                data.StoredTargetUpdatedAt);
        }

        [Fact]
        public async Task UpdateCriterion_EmptyFormula_Fail()
        {
            // Boundary: every field null cannot be resolved into any formula type
            var data = await SeedAsync();

            var result = await _handler.HandleAsync(
                new UpdateCriterionCommand
                {
                    Id = data.Target.Id,
                    Name = "Tuổi > 65",
                    Formula = new FormulaDto(),
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await AssertUnchangedAsync(
                data.Target.Id,
                "Tuổi >= 65",
                "AGE ≥ 65",
                data.StoredTargetUpdatedAt);
        }

        [Fact]
        public async Task UpdateCriterion_InvalidFormula_Fail()
        {
            // Boolean and numeric operands make the domain model throw, which the
            // mapper reports as a failure instead of storing a broken criterion
            var data = await SeedAsync();

            var result = await _handler.HandleAsync(
                new UpdateCriterionCommand
                {
                    Id = data.Target.Id,
                    Name = "Age in years",
                    Formula = Binary(Constant("true"), ExpressionOperator.ADD, Constant("65")),
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await AssertUnchangedAsync(
                data.Target.Id,
                "Tuổi >= 65",
                "AGE ≥ 65",
                data.StoredTargetUpdatedAt);
        }

        #endregion

        private static async Task AssertNotDeletedAsync<T>(DbSet<T> set, Guid id)
            where T : Base
        {
            var entity = await set.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
            Assert.False(entity.IsDeleted);
            Assert.Null(entity.DeletedAt);
        }
    }
}
