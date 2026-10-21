using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Criteria.GetCriterionById;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;
using FormulaDto = Respira.Clinical.Application.Features.Shared.ManageFormula.FormulaDto;

namespace Respira.Clinical.Application.Test.Features.Criteria.GetCriterionById
{
    public class GetCriterionByIdHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly GetCriterionByIdHandler _handler;
        private readonly IDbContext _context;

        public GetCriterionByIdHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var mapper = new FormulaModelMapper();
            var logger = new Mock<ILogger<GetCriterionByIdHandler>>().Object;

            // Initialize handler
            _handler = new(_context, mapper, logger);
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
            await _context.RiskFactors.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Treatments.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Criteria.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        /*
         * The clinical variables the criterion formulas below are built from. The
         * thresholds mirror the real CURB-65 pneumonia score: age >= 65 years and
         * hypotension systolic blood pressure < 90 mmHg.
         */
        private static readonly VariableRef Age = new(Guid.CreateVersion7(), "AGE", ClinicalValueType.Numeric);
        private static readonly VariableRef Sbp = new(Guid.CreateVersion7(), "SBP", ClinicalValueType.Numeric);
        private static readonly VariableRef Pregnant = new(Guid.CreateVersion7(), "PREGNANT", ClinicalValueType.Boolean);

        // CURB-65: age >= 65 years
        private static Formula AgeFormula() =>
            new BinaryFormula(new VariableFormula(Age), new NumericConstantFormula(65m), ExpressionOperator.Gte);

        // Hypotension: systolic blood pressure < 90 mmHg
        private static Formula HypotensionFormula() =>
            new BinaryFormula(new VariableFormula(Sbp), new NumericConstantFormula(90m), ExpressionOperator.Lt);

        // (AGE >= 65) AND (SBP < 90)
        private static Formula CombinedFormula() =>
            new BinaryFormula(AgeFormula(), HypotensionFormula(), ExpressionOperator.And);

        // NOT PREGNANT
        private static Formula NotPregnantFormula() => new UnaryFormula(new VariableFormula(Pregnant));

        private async Task<Criterion> SeedCriterionAsync(
            string name, Formula formula, bool isDeleted = false)
        {
            var criterion = new Criterion(name, formula)
            {
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.Criteria.AddAsync(criterion, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return criterion;
        }

        #region Happy path

        [Fact]
        public async Task GetCriterionById_ReturnsCriterion_Success()
        {
            var criterion = await SeedCriterionAsync("Advanced age", AgeFormula());

            var result = await _handler.HandleAsync(
                new GetCriterionByIdQuery { Id = criterion.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(criterion.Id, result.Data.Id);
            Assert.Equal("Advanced age", result.Data.Name);

            // Business rule: the read exposes the formula twice - rendered into its
            // human readable form for display, and as the structured DTO tree so the
            // client can inspect or rebuild the expression
            Assert.Equal("AGE ≥ 65", result.Data.FormulaString);

            var formula = result.Data.Formula;
            Assert.Equal(ExpressionOperator.Gte, formula.Operator);
            Assert.Equal(Age.Id, Assert.IsType<FormulaDto>(formula.Left).Variable);
            Assert.Equal("65", Assert.IsType<FormulaDto>(formula.Right).Constant);

            // Business rule: a binary DTO carries its operator plus both operands and
            // none of the constant/variable/unary/ternary-only fields
            Assert.Null(formula.Constant);
            Assert.Null(formula.Variable);
            Assert.Null(formula.Operand);
            Assert.Null(formula.Condition);
            Assert.Null(formula.IfTrue);
            Assert.Null(formula.IfFalse);
        }

        [Fact]
        public async Task GetCriterionById_NestedFormula_RenderedWithParentheses_Success()
        {
            // CURB-65 combination: (AGE >= 65) AND (SBP < 90)
            var criterion = await SeedCriterionAsync("CURB-65 age and hypotension", CombinedFormula());

            var result = await _handler.HandleAsync(
                new GetCriterionByIdQuery { Id = criterion.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            // Business rule from Formula.Parenthesize: only leaves (constants and
            // variables) stay bare, every other operand is wrapped in parentheses so
            // the rendered string stays unambiguous
            Assert.Equal("(AGE ≥ 65) AND (SBP < 90)", result.Data.FormulaString);

            var formula = result.Data.Formula;
            Assert.Equal(ExpressionOperator.And, formula.Operator);

            var left = Assert.IsType<FormulaDto>(formula.Left);
            Assert.Equal(ExpressionOperator.Gte, left.Operator);
            Assert.Equal(Age.Id, Assert.IsType<FormulaDto>(left.Left).Variable);
            Assert.Equal("65", Assert.IsType<FormulaDto>(left.Right).Constant);

            var right = Assert.IsType<FormulaDto>(formula.Right);
            Assert.Equal(ExpressionOperator.Lt, right.Operator);
            Assert.Equal(Sbp.Id, Assert.IsType<FormulaDto>(right.Left).Variable);
            Assert.Equal("90", Assert.IsType<FormulaDto>(right.Right).Constant);
        }

        [Fact]
        public async Task GetCriterionById_BooleanConstantFormula_Success()
        {
            // Lower boundary of formula complexity: the simplest legal criterion,
            // a bare boolean constant (the Criterion constructor requires a boolean
            // result type, so this is the minimal stored formula)
            var criterion = await SeedCriterionAsync("Always satisfied", new BooleanConstantFormula(true));

            var result = await _handler.HandleAsync(
                new GetCriterionByIdQuery { Id = criterion.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(criterion.Id, result.Data.Id);
            Assert.Equal("Always satisfied", result.Data.Name);

            // The domain renders the boolean lowercase ("true"), while the DTO wire
            // format is bool.ToString() ("True") - both are the expected representations
            Assert.Equal("true", result.Data.FormulaString);
            Assert.Equal("True", result.Data.Formula.Constant);
            Assert.Null(result.Data.Formula.Variable);
            Assert.Null(result.Data.Formula.Operator);
        }

        [Fact]
        public async Task GetCriterionById_UnaryFormula_Success()
        {
            var criterion = await SeedCriterionAsync("Not pregnant", NotPregnantFormula());

            var result = await _handler.HandleAsync(
                new GetCriterionByIdQuery { Id = criterion.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal("NOT PREGNANT", result.Data.FormulaString);

            // Business rule: negation is implied by the DTO shape (Operand present,
            // Operator absent) - the Not operator itself is not carried on the wire
            Assert.Null(result.Data.Formula.Operator);
            Assert.Equal(Pregnant.Id, Assert.IsType<FormulaDto>(result.Data.Formula.Operand).Variable);
        }

        [Fact]
        public async Task GetCriterionById_DuplicateNames_ResolvedById_Success()
        {
            // There is no uniqueness check on a criterion name (see CreateCriterion),
            // so two rows may share a name and the read must return the exact row
            // identified by the requested id, not just any name match
            var first = await SeedCriterionAsync("Sepsis suspicion", HypotensionFormula());
            var second = await SeedCriterionAsync("Sepsis suspicion", AgeFormula());

            var result = await _handler.HandleAsync(
                new GetCriterionByIdQuery { Id = second.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(second.Id, result.Data.Id);
            Assert.NotEqual(first.Id, result.Data.Id);
            Assert.Equal("AGE ≥ 65", result.Data.FormulaString);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task GetCriterionById_UnknownId_Fail()
        {
            var result = await _handler.HandleAsync(
                new GetCriterionByIdQuery { Id = Guid.CreateVersion7() },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetCriterionById_EmptyId_Fail()
        {
            // Boundary: Guid.Empty can never match a stored criterion
            var result = await _handler.HandleAsync(
                new GetCriterionByIdQuery { Id = Guid.Empty },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetCriterionById_SoftDeletedCriterion_Fail()
        {
            // The !IsDeleted query filter hides soft deleted rows, so reading one is
            // reported exactly like an unknown id
            var criterion = await SeedCriterionAsync(
                "Archived screening", new BooleanConstantFormula(true), isDeleted: true);

            var result = await _handler.HandleAsync(
                new GetCriterionByIdQuery { Id = criterion.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetCriterionById_MapperFailure_PropagatesError_Fail()
        {
            // The handler must surface the mapper's error unchanged and return no data.
            // NOTE: the real FormulaModelMapper never returns a failure Result for the
            // seven known formula kinds (it throws for unknown kinds instead), so this
            // branch can only be reached through another IMapper implementation - the
            // handler contract still requires it to propagate correctly
            var criterion = await SeedCriterionAsync("Advanced age", AgeFormula());
            var handler = new GetCriterionByIdHandler(
                _context, new FailingFormulaMapper(), new Mock<ILogger<GetCriterionByIdHandler>>().Object);

            var result = await handler.HandleAsync(
                new GetCriterionByIdQuery { Id = criterion.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        /*
         * Skipped path: an unknown Formula subtype nested in a stored formula cannot be
         * reached end-to-end - the JSONB column only deserializes into the seven
         * polymorphic kinds registered on Formula, so the mapper's ArgumentException
         * throw can never fire for a database-sourced formula (covered by
         * FormulaModelMapperTest instead).
         */

        #endregion

        /// <summary>
        /// Stands in for an IMapper implementation whose mapping fails, used to exercise
        /// the handler's mapper-failure propagation branch.
        /// </summary>
        private sealed class FailingFormulaMapper : IMapper<Formula, FormulaDto>
        {
            public Result<FormulaDto> Map(Formula source, object? dependencies = null)
            {
                return Result<FormulaDto>.Failure(
                    new ApplicationError(ApplicationStatus.BadRequest, "Formula cannot be mapped"));
            }
        }
    }
}
