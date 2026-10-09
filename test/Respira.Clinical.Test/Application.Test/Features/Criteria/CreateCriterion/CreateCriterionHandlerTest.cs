using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.Criteria.CreateCriterion;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;
using FormulaDto = Respira.Clinical.Application.Features.Shared.ManageFormula.FormulaDto;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Application.Test.Features.Criteria.CreateCriterion
{
    public class CreateCriterionHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly CreateCriterionHandler _handler;
        private readonly IDbContext _context;

        public CreateCriterionHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var mapper = new CreateCriterionMapper(new FormulaMapper());
            var logger = new Mock<ILogger<CreateCriterionHandler>>().Object;

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
            await _context.Criteria.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.ClinicalVariables.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        /*
         * Seeds the clinical variables the criterion formulas below are built from:
         * LOINC 8480-6 systolic blood pressure (accepted 0-300 mmHg, normal 90-140 mmHg),
         * LOINC 1988-5 C reactive protein (accepted 0-500 mg/L, normal 0-5 mg/L) and
         * LOINC 46098-0 female sex
         */
        private async Task<(NumericClinicalVariable Sbp, NumericClinicalVariable Crp, BooleanClinicalVariable Female)> SeedVariablesAsync(
            bool softDeleteFemale = false)
        {
            var sbp = new NumericClinicalVariable
            {
                Code = "8480-6",
                Name = "Systolic blood pressure",
                Description = "Systolic blood pressure measured at the arm",
                CanonicalUnit = "mmHg",
                IsRequired = true,
                Category = ClinicalVariableCategory.Clinical,
                AcceptedRange = CreateRange(0, 300, "mmHg"),
                NormalRange = CreateRange(90, 140, "mmHg"),
            };
            var crp = new NumericClinicalVariable
            {
                Code = "1988-5",
                Name = "C reactive protein",
                Description = "C reactive protein mass per volume in serum or plasma",
                CanonicalUnit = "mg/L",
                IsRequired = false,
                Category = ClinicalVariableCategory.Paraclinical,
                AcceptedRange = CreateRange(0, 500, "mg/L"),
                NormalRange = CreateRange(0, 5, "mg/L"),
            };
            var female = new BooleanClinicalVariable
            {
                Code = "46098-0",
                Name = "Female sex",
                Description = "Whether the patient is female",
                IsRequired = true,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsDeleted = softDeleteFemale,
                DeletedAt = softDeleteFemale ? DateTimeOffset.UtcNow : null,
            };

            await _context.ClinicalVariables.AddRangeAsync(
                [sbp, crp, female], TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            return (sbp, crp, female);
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

        #region Happy path

        [Fact]
        public async Task CreateCriterion_BooleanConstant_Success()
        {
            var command = new CreateCriterionCommand
            {
                Name = "Universal triage screening",
                Formula = Constant("true"),
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.NotEqual(Guid.Empty, result.Data.Id);

            // Verify through a fresh context so the change tracker cannot mask a failed commit
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.Criteria
                .SingleAsync(x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);

            Assert.Equal("Universal triage screening", saved.Name);
            Assert.False(saved.IsDeleted);
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
        public async Task CreateCriterion_NumericBoundaryFormula_Success(string threshold)
        {
            var (sbp, _, _) = await SeedVariablesAsync();
            var command = new CreateCriterionCommand
            {
                Name = "Hypertension crisis",
                Formula = Binary(Variable(sbp.Id), ExpressionOperator.GT, Constant(threshold)),
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.Criteria
                .SingleAsync(x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);

            // The threshold must survive the jsonb round trip exactly as supplied
            var formula = Assert.IsType<BinaryFormula>(saved.Formula);
            Assert.Equal(ExpressionOperator.GT, formula.Operator);
            Assert.Equal(
                decimal.Parse(threshold, CultureInfo.InvariantCulture),
                Assert.IsType<NumericConstantFormula>(formula.Right).Constant);
            Assert.Equal(ExpressionResultType.Boolean, formula.ResultType);
            Assert.Equal($"8480-6 > {threshold}", formula.ToString());

            var variable = Assert.Single(saved.Variables);
            Assert.Equal("8480-6", variable.Code);
            Assert.Equal(ClinicalValueType.Numeric, variable.ValueType);
        }

        [Fact]
        public async Task CreateCriterion_NumericRealisticThreshold_Success()
        {
            // LOINC 1988-5: a C reactive protein above 10 mg/L signals significant
            // systemic inflammation, the standard clinical cut-off
            var (_, crp, _) = await SeedVariablesAsync();
            var command = new CreateCriterionCommand
            {
                Name = "Significant systemic inflammation",
                Formula = Binary(Variable(crp.Id), ExpressionOperator.GT, Constant("10")),
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.Criteria
                .SingleAsync(x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);

            var formula = Assert.IsType<BinaryFormula>(saved.Formula);
            Assert.Equal(10m, Assert.IsType<NumericConstantFormula>(formula.Right).Constant);
            Assert.Equal("1988-5 > 10", formula.ToString());
        }

        [Fact]
        public async Task CreateCriterion_LogicalFormula_Success()
        {
            // Female sex AND systolic blood pressure above the hypertension threshold
            var (sbp, _, female) = await SeedVariablesAsync();
            var command = new CreateCriterionCommand
            {
                Name = "Female hypertensive crisis",
                Formula = Binary(
                    Variable(female.Id),
                    ExpressionOperator.AND,
                    Binary(Variable(sbp.Id), ExpressionOperator.GT, Constant("140"))),
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.Criteria
                .SingleAsync(x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);

            var formula = Assert.IsType<BinaryFormula>(saved.Formula);
            Assert.Equal(ExpressionOperator.AND, formula.Operator);
            Assert.Equal(ExpressionResultType.Boolean, formula.ResultType);
            Assert.Equal("46098-0 AND (8480-6 > 140)", formula.ToString());

            // Both variables are still referenced once each after the round trip
            Assert.Equal(2, saved.Variables.Count());
            Assert.Contains(saved.Variables, x => x.Code == "46098-0");
            Assert.Contains(saved.Variables, x => x.Code == "8480-6");
        }

        [Fact]
        public async Task CreateCriterion_DuplicateNameAndFormula_Success()
        {
            /*
             * Business rule from the handler: there is deliberately no uniqueness check
             * on a criterion, because the same criterion can be referenced from several
             * places so that updating one reference does not disturb the others
             */
            var (sbp, _, _) = await SeedVariablesAsync();
            FormulaDto formula = Binary(Variable(sbp.Id), ExpressionOperator.GT, Constant("140"));

            var first = await _handler.HandleAsync(
                new CreateCriterionCommand { Name = "Hypertension crisis", Formula = formula },
                TestContext.Current.CancellationToken);
            var second = await _handler.HandleAsync(
                new CreateCriterionCommand { Name = "Hypertension crisis", Formula = formula },
                TestContext.Current.CancellationToken);

            Assert.True(first.IsSuccess());
            Assert.True(second.IsSuccess());
            Assert.Equal(ApplicationStatus.Success, first.StatusCode);
            Assert.Equal(ApplicationStatus.Success, second.StatusCode);
            Assert.NotNull(first.Data);
            Assert.NotNull(second.Data);

            // Two independent rows, so a later update of one of them cannot affect the other
            Assert.NotEqual(first.Data.Id, second.Data.Id);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(2, await freshContext.Criteria
                .CountAsync(x => x.Name == "Hypertension crisis", TestContext.Current.CancellationToken));
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task CreateCriterion_UnknownVariable_Fail()
        {
            var command = new CreateCriterionCommand
            {
                Name = "Female-specific risk",
                Formula = Variable(Guid.CreateVersion7()),
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            // Nothing is persisted when the formula cannot be resolved
            Assert.Equal(0, await _context.Criteria.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateCriterion_SoftDeletedVariable_Fail()
        {
            // A soft-deleted variable is hidden by the query filter, so referencing it
            // must be rejected just like an unknown variable
            var (_, _, female) = await SeedVariablesAsync(softDeleteFemale: true);

            var command = new CreateCriterionCommand
            {
                Name = "Female-specific risk",
                Formula = Variable(female.Id),
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            Assert.Equal(0, await _context.Criteria.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateCriterion_EmptyFormula_Fail()
        {
            var command = new CreateCriterionCommand
            {
                Name = "Hypertension crisis",
                Formula = new FormulaDto(),
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            Assert.Equal(0, await _context.Criteria.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateCriterion_InvalidFormula_Fail()
        {
            // Boolean and numeric operands make the domain model throw, which the
            // mapper reports as a failure instead of writing a broken criterion
            var command = new CreateCriterionCommand
            {
                Name = "Hypertension crisis",
                Formula = Binary(Constant("true"), ExpressionOperator.ADD, Constant("140")),
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            Assert.Equal(0, await _context.Criteria.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateCriterion_NonBooleanFormula_Fail()
        {
            // Business rule: a criterion must evaluate to a boolean value, so a purely
            // numeric formula cannot be turned into a criterion
            var (sbp, _, _) = await SeedVariablesAsync();

            var command = new CreateCriterionCommand
            {
                Name = "Systolic blood pressure reading",
                Formula = Binary(Variable(sbp.Id), ExpressionOperator.ADD, Constant("140")),
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            Assert.Equal(0, await _context.Criteria.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        #endregion
    }
}
