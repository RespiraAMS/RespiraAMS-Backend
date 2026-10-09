using Respira.Clinical.Application.Features.Criteria.UpdateCriterion;
using Respira.Clinical.Domain.Enums;
using FormulaDto = Respira.Clinical.Application.Features.Shared.ManageFormula.FormulaDto;

namespace Respira.Application.Test.Features.Criteria.UpdateCriterion
{
    public class UpdateCriterionValidatorTest
    {
        private readonly UpdateCriterionValidator _validator = new();

        #region Valid command

        // Boundary: a UUID v7 is the usual shape, plus the lowest and highest
        // non-empty GUID values that NotEmpty still accepts
        public static readonly TheoryData<Guid> ValidIds =
        [
            Guid.CreateVersion7(),
            new Guid("00000000-0000-0000-0000-000000000001"),
            new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
        ];

        [Theory]
        [MemberData(nameof(ValidIds))]
        public async Task UpdateCriterion_ValidId_Success(Guid id)
        {
            var command = new UpdateCriterionCommand
            {
                Id = id,
                Name = "Tuổi >= 65",
                Formula = new FormulaDto { Variable = Guid.CreateVersion7() },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Theory]
        // Boundary: a single character is the smallest value NotEmpty accepts
        [InlineData("H")]
        [InlineData("Tuổi >= 65")]
        [InlineData("Huyết áp tâm thu < 90 mmHg")]
        public async Task UpdateCriterion_ValidName_Success(string name)
        {
            var command = new UpdateCriterionCommand
            {
                Id = Guid.CreateVersion7(),
                Name = name,
                Formula = new FormulaDto { Constant = "true" },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpdateCriterion_ComparisonFormula_Success()
        {
            var command = new UpdateCriterionCommand
            {
                Id = Guid.CreateVersion7(),
                Name = "Hypotension",
                Formula = new FormulaDto
                {
                    Operator = ExpressionOperator.LT,
                    Left = new FormulaDto { Variable = Guid.CreateVersion7() },
                    Right = new FormulaDto { Constant = "90" },
                },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpdateCriterion_NestedLogicalFormula_Success()
        {
            var command = new UpdateCriterionCommand
            {
                Id = Guid.CreateVersion7(),
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
        public async Task UpdateCriterion_UnaryAndTernaryFormulas_Success()
        {
            var unary = new UpdateCriterionCommand
            {
                Id = Guid.CreateVersion7(),
                Name = "Not female",
                Formula = new FormulaDto { Operand = new FormulaDto { Constant = "true" } },
            };
            var ternary = new UpdateCriterionCommand
            {
                Id = Guid.CreateVersion7(),
                Name = "Adjusted blood pressure",
                Formula = new FormulaDto
                {
                    Condition = new FormulaDto { Variable = Guid.CreateVersion7() },
                    // The domain forbids boolean branches on a ternary
                    IfTrue = new FormulaDto { Constant = "140" },
                    IfFalse = new FormulaDto { Constant = "90" },
                },
            };

            var unaryResult = await _validator.ValidateAsync(unary, TestContext.Current.CancellationToken);
            var ternaryResult = await _validator.ValidateAsync(ternary, TestContext.Current.CancellationToken);

            Assert.Empty(unaryResult.Errors);
            Assert.Empty(ternaryResult.Errors);
        }

        #endregion

        #region Invalid id

        [Fact]
        public async Task UpdateCriterion_EmptyId_Fail()
        {
            // Boundary: Guid.Empty is the only value rejected by NotEmpty on a Guid
            var command = new UpdateCriterionCommand
            {
                Id = Guid.Empty,
                Name = "Tuổi >= 65",
                Formula = new FormulaDto { Constant = "true" },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Id", result.Errors[0].PropertyName);
        }

        #endregion

        #region Invalid name

        [Theory]
        [InlineData("")]
        // Whitespace-only value is treated as empty by NotEmpty
        [InlineData("   ")]
        public async Task UpdateCriterion_EmptyName_Fail(string name)
        {
            var command = new UpdateCriterionCommand
            {
                Id = Guid.CreateVersion7(),
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
        public async Task UpdateCriterion_EmptyIdAndNameAndFormula_Fail(string name)
        {
            // Every rule of the validator fires at once
            var command = new UpdateCriterionCommand
            {
                Id = Guid.Empty,
                Name = name,
                Formula = new FormulaDto(),
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Equal(3, result.Errors.Count);
            Assert.Contains(result.Errors, x => x.PropertyName == "Id");
            Assert.Contains(result.Errors, x => x.PropertyName == "Name");
            Assert.Contains(result.Errors, x => x.PropertyName == "Formula");
        }

        #endregion

        #region Invalid formula

        [Fact]
        public async Task UpdateCriterion_EmptyFormula_Fail()
        {
            // Boundary: a FormulaDto where every field is null is rejected by the
            // at least one field must present rule
            var command = new UpdateCriterionCommand
            {
                Id = Guid.CreateVersion7(),
                Name = "Tuổi >= 65",
                Formula = new FormulaDto(),
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Formula", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateCriterion_MixedShapeFormula_Fail()
        {
            var command = new UpdateCriterionCommand
            {
                Id = Guid.CreateVersion7(),
                Name = "Tuổi >= 65",
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

        [Fact]
        public async Task UpdateCriterion_BinaryFormulaMissingRightOperand_Fail()
        {
            var command = new UpdateCriterionCommand
            {
                Id = Guid.CreateVersion7(),
                Name = "Hypotension",
                Formula = new FormulaDto
                {
                    Left = new FormulaDto { Variable = Guid.CreateVersion7() },
                    Operator = ExpressionOperator.LT,
                    Right = null,
                },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Formula", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateCriterion_EmptyConstant_Fail()
        {
            var command = new UpdateCriterionCommand
            {
                Id = Guid.CreateVersion7(),
                Name = "Universal screening",
                Formula = new FormulaDto { Constant = "" },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Formula.Constant", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateCriterion_EmptyGuidVariable_Fail()
        {
            var command = new UpdateCriterionCommand
            {
                Id = Guid.CreateVersion7(),
                Name = "Female-specific risk",
                Formula = new FormulaDto { Variable = Guid.Empty },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Formula.Variable", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateCriterion_OperatorNotInEnum_Fail()
        {
            // Boundary: 999 is outside every defined ExpressionOperator member
            var command = new UpdateCriterionCommand
            {
                Id = Guid.CreateVersion7(),
                Name = "Hypotension",
                Formula = new FormulaDto
                {
                    Operator = (ExpressionOperator)999,
                    Left = new FormulaDto { Variable = Guid.CreateVersion7() },
                    Right = new FormulaDto { Constant = "90" },
                },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Formula.Operator", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateCriterion_EmptyLeftOperand_Fail()
        {
            var command = new UpdateCriterionCommand
            {
                Id = Guid.CreateVersion7(),
                Name = "Hypotension",
                Formula = new FormulaDto
                {
                    Operator = ExpressionOperator.LT,
                    Left = new FormulaDto(),
                    Right = new FormulaDto { Constant = "90" },
                },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Formula.Left", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateCriterion_EmptyUnaryOperand_Fail()
        {
            var command = new UpdateCriterionCommand
            {
                Id = Guid.CreateVersion7(),
                Name = "Not female",
                Formula = new FormulaDto { Operand = new FormulaDto() },
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Formula.Operand", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateCriterion_EmptyTernaryCondition_Fail()
        {
            var command = new UpdateCriterionCommand
            {
                Id = Guid.CreateVersion7(),
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
