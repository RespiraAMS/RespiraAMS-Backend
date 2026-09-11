using Respira.Domain.Enums;
using Respira.Domain.Models;
using Xunit;

namespace Respira.Domain.Test.Models
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
        [InlineData(ExpressionOperator.ADD, 1, 2, 3)]
        [InlineData(ExpressionOperator.SUB, 1, 2, -1)]
        [InlineData(ExpressionOperator.MUL, 1, 2, 2)]
        [InlineData(ExpressionOperator.DIV, 1, 2, 0.5)]
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
            Assert.Throws<ArgumentException>(() => new BinaryExpression(leftOperand, rightOperand, ExpressionOperator.DIV));
        }

        [Theory]
        [InlineData(ExpressionOperator.GT, 2, 1, true)]
        [InlineData(ExpressionOperator.GT, 2, 2, false)]
        [InlineData(ExpressionOperator.GT, 2, 3, false)]
        [InlineData(ExpressionOperator.LT, 2, 1, false)]
        [InlineData(ExpressionOperator.LT, 2, 2, false)]
        [InlineData(ExpressionOperator.LT, 2, 3, true)]
        [InlineData(ExpressionOperator.GTE, 2, 1, true)]
        [InlineData(ExpressionOperator.GTE, 2, 2, true)]
        [InlineData(ExpressionOperator.GTE, 2, 3, false)]
        [InlineData(ExpressionOperator.LTE, 2, 1, false)]
        [InlineData(ExpressionOperator.LTE, 2, 2, true)]
        [InlineData(ExpressionOperator.LTE, 2, 3, true)]
        [InlineData(ExpressionOperator.EQ, 2, 2, true)]
        [InlineData(ExpressionOperator.EQ, 2, 1, false)]
        [InlineData(ExpressionOperator.NE, 2, 2, false)]
        [InlineData(ExpressionOperator.NE, 2, 1, true)]
        public void BinaryExpressionTest_SimpleComparisonExpression_Success(ExpressionOperator op, decimal left, decimal right, bool expected)
        {
            var leftOperand = new NumericalExpression(left);
            var rightOperand = new NumericalExpression(right);
            var expression = new BinaryExpression(leftOperand, rightOperand, op);
            Assert.Equal(ExpressionResultType.Boolean, expression.ResultType);
            Assert.Equal(expected, expression.Evaluate());
        }

        [Theory]
        [InlineData(ExpressionOperator.EQ, true, true, true)]
        [InlineData(ExpressionOperator.EQ, false, false, true)]
        [InlineData(ExpressionOperator.EQ, false, true, false)]
        [InlineData(ExpressionOperator.EQ, true, false, false)]
        [InlineData(ExpressionOperator.NE, true, false, true)]
        [InlineData(ExpressionOperator.NE, false, true, true)]
        [InlineData(ExpressionOperator.NE, true, true, false)]
        [InlineData(ExpressionOperator.NE, false, false, false)]
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
                    ExpressionOperator.ADD
                ),
                ExpressionOperator.MUL
            );

            Assert.Equal(ExpressionResultType.Numeric, expression.ResultType);
            Assert.Equal(5m, expression.Evaluate());
        }

        [Fact]
        public void BinaryExpressionTest_OperandResultNotMatch_Fail()
        {
            var leftOperand = new NumericalExpression(1);
            var rightOperand = new BooleanExpression(true);
            Assert.Throws<ArgumentException>(() => new BinaryExpression(leftOperand, rightOperand, ExpressionOperator.ADD));
        }

        [Fact]
        public void BinaryExpressionTest_NumericWithLogicalOperator_Fail()
        {
            var leftOperand = new NumericalExpression(1);
            var rightOperand = new BooleanExpression(true);
            Assert.Throws<ArgumentException>(() => new BinaryExpression(leftOperand, rightOperand, ExpressionOperator.EQ));
        }

        [Fact]
        public void BinaryExpressionTest_LogicalWithNumericOperator_Fail()
        {
            var leftOperand = new BooleanExpression(true);
            var rightOperand = new NumericalExpression(1);
            Assert.Throws<ArgumentException>(() => new BinaryExpression(leftOperand, rightOperand, ExpressionOperator.EQ));
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
