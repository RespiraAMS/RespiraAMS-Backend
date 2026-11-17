using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Xunit;

namespace Respira.Clinical.Domain.Test.Models
{
    public class ExpressionTest
    {
        #region UnaryExpression

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void UnaryExpressionTest_Success(bool value)
        {
            var operand = new BooleanExpression(value);
            var expression = new UnaryExpression(operand);
            Assert.Equal(ExpressionResultType.Boolean, expression.ResultType);
            Assert.Equal(!value, expression.Evaluate());
        }

        [Fact]
        public void UnaryExpressionTest_Fail()
        {
            var operand = new NumericalExpression(1);
            Assert.Throws<ArgumentException>(() => new UnaryExpression(operand));
        }

        #endregion

        #region BinaryExpression

        [Theory]
        [InlineData(ExpressionOperator.Add, 1, 2, 3)]
        [InlineData(ExpressionOperator.Sub, 1, 2, -1)]
        [InlineData(ExpressionOperator.Mul, 1, 2, 2)]
        [InlineData(ExpressionOperator.Div, 1, 2, 0.5)]
        public void BinaryExpressionTest_SimpleArithmeticExpression_Success(ExpressionOperator op, decimal left, decimal right, decimal expected)
        {
            var leftOperand = new NumericalExpression(left);
            var rightOperand = new NumericalExpression(right);
            var expression = new BinaryExpression(leftOperand, rightOperand, op);
            Assert.Equal(ExpressionResultType.Numeric, expression.ResultType);
            Assert.Equal(expected, expression.Evaluate());
        }

        [Fact]
        public void BinaryExpressionTest_Fail_DivisionByZero()
        {
            var leftOperand = new NumericalExpression(1);
            var rightOperand = new NumericalExpression(0);
            Assert.Throws<ArgumentException>(() => new BinaryExpression(leftOperand, rightOperand, ExpressionOperator.Div));
        }

        [Theory]
        [InlineData(ExpressionOperator.Gt, 2, 1, true)]
        [InlineData(ExpressionOperator.Gt, 2, 2, false)]
        [InlineData(ExpressionOperator.Gt, 2, 3, false)]
        [InlineData(ExpressionOperator.Lt, 2, 1, false)]
        [InlineData(ExpressionOperator.Lt, 2, 2, false)]
        [InlineData(ExpressionOperator.Lt, 2, 3, true)]
        [InlineData(ExpressionOperator.Gte, 2, 1, true)]
        [InlineData(ExpressionOperator.Gte, 2, 2, true)]
        [InlineData(ExpressionOperator.Gte, 2, 3, false)]
        [InlineData(ExpressionOperator.Lte, 2, 1, false)]
        [InlineData(ExpressionOperator.Lte, 2, 2, true)]
        [InlineData(ExpressionOperator.Lte, 2, 3, true)]
        [InlineData(ExpressionOperator.Eq, 2, 2, true)]
        [InlineData(ExpressionOperator.Eq, 2, 1, false)]
        [InlineData(ExpressionOperator.Ne, 2, 2, false)]
        [InlineData(ExpressionOperator.Ne, 2, 1, true)]
        public void BinaryExpressionTest_SimpleComparisonExpression_Success(ExpressionOperator op, decimal left, decimal right, bool expected)
        {
            var leftOperand = new NumericalExpression(left);
            var rightOperand = new NumericalExpression(right);
            var expression = new BinaryExpression(leftOperand, rightOperand, op);
            Assert.Equal(ExpressionResultType.Boolean, expression.ResultType);
            Assert.Equal(expected, expression.Evaluate());
        }

        [Theory]
        [InlineData(ExpressionOperator.Eq, true, true, true)]
        [InlineData(ExpressionOperator.Eq, false, false, true)]
        [InlineData(ExpressionOperator.Eq, false, true, false)]
        [InlineData(ExpressionOperator.Eq, true, false, false)]
        [InlineData(ExpressionOperator.Ne, true, false, true)]
        [InlineData(ExpressionOperator.Ne, false, true, true)]
        [InlineData(ExpressionOperator.Ne, true, true, false)]
        [InlineData(ExpressionOperator.Ne, false, false, false)]
        public void BinaryExpressionTest_ComparisonExpression_Success(ExpressionOperator op, bool left, bool right, bool expected)
        {
            var leftOperand = new BooleanExpression(left);
            var rightOperand = new BooleanExpression(right);
            var expression = new BinaryExpression(leftOperand, rightOperand, op);
            Assert.Equal(ExpressionResultType.Boolean, expression.ResultType);
            Assert.Equal(expected, expression.Evaluate());
        }

        [Fact]
        public void BinaryExpressionTest_ComplexExpression_Success()
        {
            // 2 * (3 + 1) = 5
            var expression = new BinaryExpression(
                new NumericalExpression(1),
                new BinaryExpression(
                    new NumericalExpression(2),
                    new NumericalExpression(3),
                    ExpressionOperator.Add
                ),
                ExpressionOperator.Mul
            );

            Assert.Equal(ExpressionResultType.Numeric, expression.ResultType);
            Assert.Equal(5m, expression.Evaluate());
        }

        [Fact]
        public void BinaryExpressionTest_OperandResultNotMatch_Fail()
        {
            var leftOperand = new NumericalExpression(1);
            var rightOperand = new BooleanExpression(true);
            Assert.Throws<ArgumentException>(() => new BinaryExpression(leftOperand, rightOperand, ExpressionOperator.Add));
        }

        [Fact]
        public void BinaryExpressionTest_NumericWithLogicalOperator_Fail()
        {
            var leftOperand = new NumericalExpression(1);
            var rightOperand = new BooleanExpression(true);
            Assert.Throws<ArgumentException>(() => new BinaryExpression(leftOperand, rightOperand, ExpressionOperator.Eq));
        }

        [Fact]
        public void BinaryExpressionTest_LogicalWithNumericOperator_Fail()
        {
            var leftOperand = new BooleanExpression(true);
            var rightOperand = new NumericalExpression(1);
            Assert.Throws<ArgumentException>(() => new BinaryExpression(leftOperand, rightOperand, ExpressionOperator.Eq));
        }

        #endregion

        #region TernaryExpression

        [Fact]
        public void TernaryExpressionTest_Success()
        {
            var condition = new BooleanExpression(true);
            var ifTrue = new NumericalExpression(1);
            var ifFalse = new NumericalExpression(2);
            var expression = new TernaryExpression(condition, ifTrue, ifFalse);
            Assert.Equal(ExpressionResultType.Numeric, expression.ResultType);
            Assert.Equal(1m, expression.Evaluate());
        }

        public static TheoryData<Expression, Expression> FailPath_BranchExpressionNotNumeric = [

            new(new NumericalExpression(1),new BooleanExpression(true)),
            new(new BooleanExpression(true), new NumericalExpression(1)),
            new (new BooleanExpression(true), new BooleanExpression(false)),
        ];


        [Theory]
        [MemberData(nameof(FailPath_BranchExpressionNotNumeric))]
        public void TernaryExpressionTest_NotNumericIfTrue_Fail(Expression ifTrue, Expression ifFalse)
        {
            var condition = new BooleanExpression(true);
            Assert.Throws<ArgumentException>(() => new TernaryExpression(condition, ifTrue, ifFalse));
        }

        [Fact]
        public void TernaryExpressionTest_NotBooleanCondition_Fail()
        {
            var condition = new NumericalExpression(1);
            var ifTrue = new BooleanExpression(true);
            var ifFalse = new BooleanExpression(false);
            Assert.Throws<ArgumentException>(() => new TernaryExpression(condition, ifTrue, ifFalse));
        }

        #endregion

    }
}
