using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Enums;

namespace Respira.Application.Test.Features.Shared.ManageFormula
{
    public class FormulaValidatorTest
    {
        private const string EmptyError = "Formula cannot be empty";
        private const string ConstantShapeError = "Formula must be valid: constant formula must have constant not null and other fields are null";
        private const string VariableShapeError = "Formula must be valid: if variable is supplied, other must be null";
        private const string BinaryShapeError = "Formula must be valid: if binary formula, then left, right and operator must not null while others must be null";
        private const string UnaryShapeError = "Formula must be valid: if unary formula, then operand must not null while others must be null";
        private const string TernaryShapeError = "Formula must be valid: if ternary formula, then condition, ifTrue and ifFalse must not null while others must be null";

        private readonly FormulaValidator _validator = new();

        #region Valid formula

        [Theory]
        [InlineData("A")]
        [InlineData(" ")]
        [InlineData("SEVERE")]
        [InlineData("3.5")]
        [InlineData("true")]
        public async Task Validate_Constant_Success(string constant)
        {
            var result = await _validator.ValidateAsync(
                new FormulaDto { Constant = constant },
                TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task Validate_Variable_Success()
        {
            var result = await _validator.ValidateAsync(
                new FormulaDto { Variable = Guid.CreateVersion7() },
                TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Theory]
        [InlineData(ExpressionOperator.ADD)]
        [InlineData(ExpressionOperator.GT)]
        [InlineData(ExpressionOperator.TERNARY)]
        public async Task Validate_Binary_Success(ExpressionOperator op)
        {
            var dto = new FormulaDto
            {
                Operator = op,
                Left = new FormulaDto { Constant = "2" },
                Right = new FormulaDto { Constant = "3" },
            };

            var result = await _validator.ValidateAsync(dto, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task Validate_Unary_Success()
        {
            var dto = new FormulaDto
            {
                Operand = new FormulaDto { Constant = "true" },
            };

            var result = await _validator.ValidateAsync(dto, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task Validate_Ternary_Success()
        {
            var dto = new FormulaDto
            {
                Condition = new FormulaDto { Constant = "true" },
                IfTrue = new FormulaDto { Constant = "1" },
                IfFalse = new FormulaDto { Constant = "2" },
            };

            var result = await _validator.ValidateAsync(dto, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task Validate_DeeplyNested_Success()
        {
            var dto = new FormulaDto
            {
                Operator = ExpressionOperator.AND,
                Left = new FormulaDto
                {
                    Operand = new FormulaDto { Variable = Guid.CreateVersion7() },
                },
                Right = new FormulaDto
                {
                    Operator = ExpressionOperator.GT,
                    Left = new FormulaDto { Variable = Guid.CreateVersion7() },
                    Right = new FormulaDto { Constant = "20" },
                },
            };

            var result = await _validator.ValidateAsync(dto, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid formula shape

        [Fact]
        public async Task Validate_EmptyFormula_Fail()
        {
            var result = await _validator.ValidateAsync(
                new FormulaDto(),
                TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(string.Empty, result.Errors[0].PropertyName);
            Assert.Equal(EmptyError, result.Errors[0].ErrorMessage);
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        [InlineData(false, true, true)]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        public async Task Validate_IncompleteBinary_Fail(bool includeOperator, bool includeLeft, bool includeRight)
        {
            // Every combination contains at least one field (so the empty rule passes),
            // but none forms a complete binary formula
            var dto = new FormulaDto
            {
                Operator = includeOperator ? ExpressionOperator.ADD : null,
                Left = includeLeft ? new FormulaDto { Constant = "1" } : null,
                Right = includeRight ? new FormulaDto { Constant = "2" } : null,
            };

            var result = await _validator.ValidateAsync(dto, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(string.Empty, result.Errors[0].PropertyName);
            Assert.Equal(BinaryShapeError, result.Errors[0].ErrorMessage);
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(true, true, false)]
        [InlineData(false, true, true)]
        public async Task Validate_IncompleteTernary_Fail(bool includeCondition, bool includeIfTrue, bool includeIfFalse)
        {
            var dto = new FormulaDto
            {
                Condition = includeCondition ? new FormulaDto { Constant = "true" } : null,
                IfTrue = includeIfTrue ? new FormulaDto { Constant = "1" } : null,
                IfFalse = includeIfFalse ? new FormulaDto { Constant = "2" } : null,
            };

            var result = await _validator.ValidateAsync(dto, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(string.Empty, result.Errors[0].PropertyName);
            Assert.Equal(TernaryShapeError, result.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Validate_MixedConstantAndVariable_Fail()
        {
            var dto = new FormulaDto
            {
                Constant = "5",
                Variable = Guid.CreateVersion7(),
            };

            var result = await _validator.ValidateAsync(dto, TestContext.Current.CancellationToken);

            Assert.Equal(2, result.Errors.Count);
            Assert.All(result.Errors, x => Assert.Equal(string.Empty, x.PropertyName));
            Assert.Contains(result.Errors, x => x.ErrorMessage == ConstantShapeError);
            Assert.Contains(result.Errors, x => x.ErrorMessage == VariableShapeError);
        }

        [Fact]
        public async Task Validate_MixedUnaryAndTernary_Fail()
        {
            var dto = new FormulaDto
            {
                Operand = new FormulaDto { Constant = "true" },
                Condition = new FormulaDto { Constant = "true" },
                IfTrue = new FormulaDto { Constant = "1" },
                IfFalse = new FormulaDto { Constant = "2" },
            };

            var result = await _validator.ValidateAsync(dto, TestContext.Current.CancellationToken);

            Assert.Equal(2, result.Errors.Count);
            Assert.Contains(result.Errors, x => x.ErrorMessage == UnaryShapeError);
            Assert.Contains(result.Errors, x => x.ErrorMessage == TernaryShapeError);
        }

        #endregion

        #region Invalid formula field

        [Fact]
        public async Task Validate_EmptyConstant_Fail()
        {
            var result = await _validator.ValidateAsync(
                new FormulaDto { Constant = string.Empty },
                TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Constant", result.Errors[0].PropertyName);
            Assert.Equal("Constant must be a non-empty string", result.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Validate_EmptyGuidVariable_Fail()
        {
            var result = await _validator.ValidateAsync(
                new FormulaDto { Variable = Guid.Empty },
                TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Variable", result.Errors[0].PropertyName);
            Assert.Equal("Variable must be a valid guid", result.Errors[0].ErrorMessage);
        }

        [Theory]
        [InlineData((ExpressionOperator)14)]
        [InlineData((ExpressionOperator)(-1))]
        public async Task Validate_OperatorNotInEnum_Fail(ExpressionOperator op)
        {
            var dto = new FormulaDto
            {
                Operator = op,
                Left = new FormulaDto { Constant = "1" },
                Right = new FormulaDto { Constant = "2" },
            };

            var result = await _validator.ValidateAsync(dto, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Operator", result.Errors[0].PropertyName);
            Assert.Equal("Operator must either be null or valid value", result.Errors[0].ErrorMessage);
        }

        #endregion

        #region Nested formula

        [Theory]
        [InlineData("Left")]
        [InlineData("Right")]
        [InlineData("Operand")]
        [InlineData("Condition")]
        [InlineData("IfTrue")]
        [InlineData("IfFalse")]
        public async Task Validate_NestedEmptyConstant_Fail(string child)
        {
            var dto = CreateWithChild(child, new FormulaDto { Constant = string.Empty });

            var result = await _validator.ValidateAsync(dto, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal($"{child}.Constant", result.Errors[0].PropertyName);
            Assert.Equal("Constant must be a non-empty string", result.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Validate_NestedEmptyFormula_Fail()
        {
            var dto = new FormulaDto
            {
                Operator = ExpressionOperator.ADD,
                Left = new FormulaDto { Constant = "1" },
                Right = new FormulaDto(),
            };

            var result = await _validator.ValidateAsync(dto, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Right", result.Errors[0].PropertyName);
            Assert.Equal(EmptyError, result.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Validate_NestedOperatorNotInEnum_Fail()
        {
            var dto = new FormulaDto
            {
                Operator = ExpressionOperator.ADD,
                Left = new FormulaDto
                {
                    Operator = (ExpressionOperator)14,
                    Left = new FormulaDto { Constant = "1" },
                    Right = new FormulaDto { Constant = "2" },
                },
                Right = new FormulaDto { Constant = "1" },
            };

            var result = await _validator.ValidateAsync(dto, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Left.Operator", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task Validate_NestedMixedShapes_Fail()
        {
            var dto = new FormulaDto
            {
                Operator = ExpressionOperator.ADD,
                Left = new FormulaDto
                {
                    Constant = "5",
                    Variable = Guid.CreateVersion7(),
                },
                Right = new FormulaDto { Constant = "1" },
            };

            var result = await _validator.ValidateAsync(dto, TestContext.Current.CancellationToken);

            Assert.Equal(2, result.Errors.Count);
            // Whole-object rules only carry the parent path, since RuleFor(x => x)
            // itself has no property name
            Assert.All(result.Errors, x => Assert.Equal("Left", x.PropertyName));
            Assert.Contains(result.Errors, x => x.ErrorMessage == ConstantShapeError);
            Assert.Contains(result.Errors, x => x.ErrorMessage == VariableShapeError);
        }

        [Fact]
        public async Task Validate_DeeplyNestedEmptyConstant_Fail()
        {
            var dto = new FormulaDto
            {
                Operator = ExpressionOperator.ADD,
                Left = new FormulaDto
                {
                    Operator = ExpressionOperator.ADD,
                    Left = new FormulaDto { Constant = string.Empty },
                    Right = new FormulaDto { Constant = "1" },
                },
                Right = new FormulaDto { Constant = "1" },
            };

            var result = await _validator.ValidateAsync(dto, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Left.Left.Constant", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task Validate_MultipleChildrenInvalid_Fail()
        {
            var dto = new FormulaDto
            {
                Operator = ExpressionOperator.ADD,
                Left = new FormulaDto { Constant = string.Empty },
                Right = new FormulaDto { Variable = Guid.Empty },
            };

            var result = await _validator.ValidateAsync(dto, TestContext.Current.CancellationToken);

            Assert.Equal(2, result.Errors.Count);
            Assert.Contains(result.Errors, x => x.PropertyName == "Left.Constant");
            Assert.Contains(result.Errors, x => x.PropertyName == "Right.Variable");
        }

        #endregion

        private static FormulaDto CreateWithChild(string child, FormulaDto childDto)
        {
            return child switch
            {
                "Left" => new FormulaDto
                {
                    Operator = ExpressionOperator.ADD,
                    Left = childDto,
                    Right = new FormulaDto { Constant = "1" },
                },
                "Right" => new FormulaDto
                {
                    Operator = ExpressionOperator.ADD,
                    Left = new FormulaDto { Constant = "1" },
                    Right = childDto,
                },
                "Operand" => new FormulaDto { Operand = childDto },
                "Condition" => new FormulaDto
                {
                    Condition = childDto,
                    IfTrue = new FormulaDto { Constant = "1" },
                    IfFalse = new FormulaDto { Constant = "2" },
                },
                "IfTrue" => new FormulaDto
                {
                    Condition = new FormulaDto { Constant = "true" },
                    IfTrue = childDto,
                    IfFalse = new FormulaDto { Constant = "2" },
                },
                "IfFalse" => new FormulaDto
                {
                    Condition = new FormulaDto { Constant = "true" },
                    IfTrue = new FormulaDto { Constant = "1" },
                    IfFalse = childDto,
                },
                _ => throw new ArgumentOutOfRangeException(nameof(child), child, "Unknown formula child"),
            };
        }
    }
}
