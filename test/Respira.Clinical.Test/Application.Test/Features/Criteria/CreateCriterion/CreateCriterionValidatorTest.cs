using Respira.Clinical.Application.Features.Criteria.CreateCriterion;
using Respira.Clinical.Domain.Enums;
using FormulaDto = Respira.Clinical.Application.Features.Shared.ManageFormula.FormulaDto;

namespace Respira.Application.Test.Features.Criteria.CreateCriterion
{
    public class CreateCriterionValidatorTest
    {
        private readonly CreateCriterionValidator _validator = new();

        #region Valid command

        [Theory]
        // Boundary: a single character is the smallest value NotEmpty accepts
        [InlineData("H")]
        [InlineData("Hypertension crisis")]
        [InlineData("Severe sepsis with organ dysfunction")]
        public async Task CreateCriterion_ValidName_Success(string name)
        {
            // LOINC 46098-0 - female sex, the simplest boolean criterion
            var command = new CreateCriterionCommand
            {
                Name = name,
                Formula = new FormulaDto { Variable = Guid.CreateVersion7() },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task CreateCriterion_BooleanConstantFormula_Success()
        {
            var command = new CreateCriterionCommand
            {
                Name = "Universal triage screening",
                Formula = new FormulaDto { Constant = "true" },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task CreateCriterion_NumericConstantBoundary_Success()
        {
            // Boundary: "0" is the smallest non-empty numeric constant, so it must
            // still satisfy the Constant must be a non-empty string rule
            var command = new CreateCriterionCommand
            {
                Name = "Blood pressure offset",
                Formula = new FormulaDto { Constant = "0" },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task CreateCriterion_ComparisonFormula_Success()
        {
            // LOINC 8480-6 - systolic blood pressure above the hypertension threshold
            var command = new CreateCriterionCommand
            {
                Name = "Hypertension crisis",
                Formula = new FormulaDto
                {
                    Operator = ExpressionOperator.GT,
                    Left = new FormulaDto { Variable = Guid.CreateVersion7() },
                    Right = new FormulaDto { Constant = "140" },
                },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task CreateCriterion_NestedLogicalFormula_Success()
        {
            // Female sex AND systolic blood pressure above 140 mmHg
            var command = new CreateCriterionCommand
            {
                Name = "Female hypertensive crisis",
                Formula = new FormulaDto
                {
                    Operator = ExpressionOperator.AND,
                    Left = new FormulaDto { Variable = Guid.CreateVersion7() },
                    Right = new FormulaDto
                    {
                        Operator = ExpressionOperator.GT,
                        Left = new FormulaDto { Variable = Guid.CreateVersion7() },
                        Right = new FormulaDto { Constant = "140" },
                    },
                },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task CreateCriterion_UnaryFormula_Success()
        {
            var command = new CreateCriterionCommand
            {
                Name = "Not female",
                Formula = new FormulaDto
                {
                    Operand = new FormulaDto { Constant = "true" },
                },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task CreateCriterion_TernaryFormula_Success()
        {
            var command = new CreateCriterionCommand
            {
                Name = "Adjusted blood pressure",
                Formula = new FormulaDto
                {
                    Condition = new FormulaDto { Variable = Guid.CreateVersion7() },
                    // The domain forbids boolean branches on a ternary, so the
                    // branches carry the numeric result instead
                    IfTrue = new FormulaDto { Constant = "140" },
                    IfFalse = new FormulaDto { Constant = "90" },
                },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid name

        [Theory]
        [InlineData("")]
        // Whitespace-only value is treated as empty by NotEmpty
        [InlineData("   ")]
        public async Task CreateCriterion_EmptyName_Fail(string name)
        {
            var command = new CreateCriterionCommand
            {
                Name = name,
                Formula = new FormulaDto { Constant = "true" },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Name", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateCriterion_EmptyNameAndFormula_Fail(string name)
        {
            var command = new CreateCriterionCommand
            {
                Name = name,
                Formula = new FormulaDto(),
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Equal(2, result.Errors.Count);
            Assert.Contains(result.Errors, x => x.PropertyName == "Name");
            Assert.Contains(result.Errors, x => x.PropertyName == "Formula");
        }

        #endregion

        #region Invalid formula shape

        [Fact]
        public async Task CreateCriterion_EmptyFormula_Fail()
        {
            // Boundary: a FormulaDto where every field is null is rejected by the
            // at least one field must present rule
            var command = new CreateCriterionCommand
            {
                Name = "Hypertension crisis",
                Formula = new FormulaDto(),
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Formula", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task CreateCriterion_MixedShapeFormula_Fail()
        {
            // A formula must be exactly one of constant / variable / binary / unary / ternary
            var command = new CreateCriterionCommand
            {
                Name = "Hypertension crisis",
                Formula = new FormulaDto
                {
                    Constant = "true",
                    Variable = Guid.CreateVersion7(),
                },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Equal(2, result.Errors.Count);
            Assert.All(result.Errors, x => Assert.Equal("Formula", x.PropertyName));
        }

        [Theory]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        public async Task CreateCriterion_BinaryFormulaMissingPart_Fail(
            bool includeLeft, bool includeRight, bool includeOperator)
        {
            var command = new CreateCriterionCommand
            {
                Name = "Hypertension crisis",
                Formula = new FormulaDto
                {
                    Left = includeLeft ? new FormulaDto { Variable = Guid.CreateVersion7() } : null,
                    Right = includeRight ? new FormulaDto { Constant = "140" } : null,
                    Operator = includeOperator ? ExpressionOperator.GT : null,
                },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Formula", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task CreateCriterion_TernaryFormulaMissingBranch_Fail()
        {
            var command = new CreateCriterionCommand
            {
                Name = "Adjusted blood pressure",
                Formula = new FormulaDto
                {
                    Condition = new FormulaDto { Constant = "true" },
                    IfTrue = new FormulaDto { Constant = "140" },
                    IfFalse = null,
                },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Formula", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task CreateCriterion_EmptyConstant_Fail()
        {
            var command = new CreateCriterionCommand
            {
                Name = "Blood pressure offset",
                Formula = new FormulaDto { Constant = "" },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Formula.Constant", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task CreateCriterion_EmptyGuidVariable_Fail()
        {
            var command = new CreateCriterionCommand
            {
                Name = "Female-specific risk",
                Formula = new FormulaDto { Variable = Guid.Empty },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Formula.Variable", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task CreateCriterion_OperatorNotInEnum_Fail()
        {
            // Boundary: 999 is outside every defined ExpressionOperator member
            var command = new CreateCriterionCommand
            {
                Name = "Hypertension crisis",
                Formula = new FormulaDto
                {
                    Operator = (ExpressionOperator)999,
                    Left = new FormulaDto { Variable = Guid.CreateVersion7() },
                    Right = new FormulaDto { Constant = "140" },
                },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Formula.Operator", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task CreateCriterion_EmptyLeftOperand_Fail()
        {
            var command = new CreateCriterionCommand
            {
                Name = "Hypertension crisis",
                Formula = new FormulaDto
                {
                    Operator = ExpressionOperator.GT,
                    Left = new FormulaDto(),
                    Right = new FormulaDto { Constant = "140" },
                },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Formula.Left", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task CreateCriterion_EmptyRightOperand_Fail()
        {
            var command = new CreateCriterionCommand
            {
                Name = "Hypertension crisis",
                Formula = new FormulaDto
                {
                    Operator = ExpressionOperator.GT,
                    Left = new FormulaDto { Variable = Guid.CreateVersion7() },
                    Right = new FormulaDto(),
                },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Formula.Right", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task CreateCriterion_EmptyUnaryOperand_Fail()
        {
            var command = new CreateCriterionCommand
            {
                Name = "Not female",
                Formula = new FormulaDto { Operand = new FormulaDto() },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Formula.Operand", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task CreateCriterion_EmptyTernaryCondition_Fail()
        {
            var command = new CreateCriterionCommand
            {
                Name = "Adjusted blood pressure",
                Formula = new FormulaDto
                {
                    Condition = new FormulaDto(),
                    IfTrue = new FormulaDto { Constant = "140" },
                    IfFalse = new FormulaDto { Constant = "90" },
                },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Formula.Condition", result.Errors[0].PropertyName);
        }

        #endregion
    }
}
