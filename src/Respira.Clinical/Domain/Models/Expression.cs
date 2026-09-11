using Respira.Domain.Entities;
using Respira.Domain.Enums;

namespace Respira.Domain.Models
{
    /// <summary>
    /// Abstract expression: this class represent an expression.
    /// This can be used to evaluate a medical criterion or a score calculation for scoring metrics
    /// like CURB-65 or PSI
    /// </summary>
    public abstract class Expression
    {
        /// <summary>
        /// The result type of the expression
        /// </summary>
        public abstract ExpressionResultType ResultType { get; }

        /// <summary>
        /// Evaluate the expression
        /// </summary>
        /// <returns>
        /// The expression result. Since this is an abstraction, we return an object here.
        /// The classes implemented must return correct data type according to the ResultType,
        /// and caller also responsible to check for type casting before using the result
        /// </returns>
        public abstract object Evaluate();
    }

    /// <summary>
    /// This is used to accept a numeric value for a constant operand in a more complex expression.
    /// </summary>
    /// <param name="value">
    /// The numerical value. Even though the value type is object, but it self to be parsed into decimal
    /// </param>
    public class NumericalExpression(decimal value) : Expression
    {
        public override ExpressionResultType ResultType => ExpressionResultType.Numeric;

        public override object Evaluate()
        {
            return value;
        }
    }

    /// <summary>
    /// This used to accept a boolean value for a constant boolean operand.
    /// </summary>
    /// <param name="value">
    /// The boolean value. Even though the value type is object, but it self to be parsed into boolean
    /// </param>
    public class BooleanExpression(bool value) : Expression
    {
        public override ExpressionResultType ResultType => ExpressionResultType.Boolean;

        public override object Evaluate()
        {
            return value;
        }
    }

    /// <summary>
    /// This is used to accept a clinical observation for a variable operand.
    /// </summary>
    /// <param name="observation">
    /// Observable value. The return value is object, but it safe to be parse to either boolean or decimal
    /// by checking the ResultType
    /// </param>
    public class ObservationExpression(ClinicalObservation observation) : Expression
    {
        /// <summary>
        /// Clinical variable that this expression using
        /// </summary>
        public ClinicalVariable Variable { get; init; } = observation.Variable;

        public override ExpressionResultType ResultType => Variable.ValueType == ClinicalValueType.Boolean ? ExpressionResultType.Boolean : ExpressionResultType.Numeric;

        public override object Evaluate()
        {
            // The null check is ClinicalObservation responsibility
            if (Variable.ValueType == ClinicalValueType.Boolean)
            {
                return observation.BooleanValue!;
            }

            return observation.NumericValue!;
        }
    }

    /// <summary>
    /// Unary expression only support boolean (NOT operator). For the mathematical unary operator (negation),
    /// we won't support it since it is not a commonly used operator, and a simple 0 - x would also work
    /// </summary>
    public class UnaryExpression : Expression
    {
        public override ExpressionResultType ResultType => ExpressionResultType.Boolean;

        public Expression Operand { get; set; }

        public UnaryExpression(Expression operand)
        {
            if (operand.ResultType != ExpressionResultType.Boolean)
            {
                throw new ArgumentException("Unary expression only support boolean operand");
            }
            Operand = operand;
        }

        public override object Evaluate()
        {
            return !(bool)Operand.Evaluate();
        }
    }

    /// <summary>
    /// This is the most used expression. It can support both boolean and numeric operand,
    /// and support mathematical operator and logical operator
    /// </summary>
    public class BinaryExpression : Expression
    {
        public override ExpressionResultType ResultType => Operator.IsBooleanResult() ? ExpressionResultType.Boolean : ExpressionResultType.Numeric;

        /// <summary>
        /// Left operand
        /// </summary>
        public Expression Left { get; set; }

        /// <summary>
        /// Right operand
        /// </summary>
        public Expression Right { get; set; }

        /// <summary>
        /// Operator
        /// </summary>
        public ExpressionOperator Operator { get; set; }

        public BinaryExpression(Expression left, Expression right, ExpressionOperator op)
        {
            if (left.ResultType != right.ResultType)
            {
                throw new ArgumentException($"Operand type mismatch: (left) {left.ResultType} | (right) {right.ResultType}");
            }

            if (left.ResultType == ExpressionResultType.Boolean && !op.IsLogicalOperator() && op != ExpressionOperator.EQ && op != ExpressionOperator.NE)
            {
                throw new ArgumentException($"Invalid expression: applying non logical operator to boolean operand: result {left.ResultType} | operator {op}");
            }

            if (left.ResultType == ExpressionResultType.Numeric && !op.IsMathematicalOperator())
            {
                throw new ArgumentException($"Invalid expression: applying non mathematical operator to numeric operand: result {left.ResultType} | operator {op}");
            }

            // Check for division by zero.
            if (op == ExpressionOperator.DIV && right.ResultType == ExpressionResultType.Numeric && (decimal)right.Evaluate() == 0)
            {
                throw new ArgumentException("Division by zero");
            }

            Left = left;
            Right = right;
            Operator = op;
        }

        public override object Evaluate()
        {
            if (Operator.IsLogicalOperator())
            {
                return Operator switch
                {
                    ExpressionOperator.AND => (bool)Left.Evaluate() && (bool)Right.Evaluate(),
                    ExpressionOperator.OR => (bool)Left.Evaluate() || (bool)Right.Evaluate(),
                    // NOT is handle by UnaryExpression
                    _ => throw new Exception($"Unexpected logical operator: {Operator}"),
                };
            }

            // Since EQUAL and NOT EQUAL can operate on both boolean and numeric operand,
            // we need to check the result type of left and right
            if (Left.ResultType == ExpressionResultType.Boolean && Right.ResultType == ExpressionResultType.Boolean)
            {
                return Operator switch
                {
                    ExpressionOperator.EQ => (bool)Left.Evaluate() == (bool)Right.Evaluate(),
                    ExpressionOperator.NE => (bool)Left.Evaluate() != (bool)Right.Evaluate(),
                    _ => throw new Exception($"Unexpected logical operator: {Operator}"),
                };
            }

            // Numeric expression
            var left = (decimal)Left.Evaluate();
            var right = (decimal)Right.Evaluate();

            return Operator switch
            {
                ExpressionOperator.ADD => left + right,
                ExpressionOperator.SUB => left - right,
                ExpressionOperator.MUL => left * right,
                ExpressionOperator.DIV => left / right,
                ExpressionOperator.GT => left > right,
                ExpressionOperator.LT => left < right,
                ExpressionOperator.GTE => left >= right,
                ExpressionOperator.LTE => left <= right,
                ExpressionOperator.EQ => left == right,
                ExpressionOperator.NE => left != right,
                _ => throw new ArgumentException($"Unexpected mathematical operator: {nameof(Operator)}"),
            };
        }

    }

    /// <summary>
    /// Ternary expression. This expression only supported for the result type of Numeric
    /// </summary>
    public class TernaryExpression : Expression
    {
        public override ExpressionResultType ResultType => ExpressionResultType.Numeric;

        /// <summary>
        /// Condition expression
        /// </summary>
        public Expression Condition { get; set; }

        /// <summary>
        /// If true expression
        /// </summary>
        public Expression IfTrue { get; set; }

        /// <summary>
        /// If false expression
        /// </summary>
        public Expression IfFalse { get; set; }

        public TernaryExpression(Expression condition, Expression ifTrue, Expression ifFalse)
        {
            // A ternary expression is:
            // 1. Condition must be boolean expression
            // 2. The expressions for both true and false branch must be non-boolean expressions
            // (We forbid it in project)
            if (condition.ResultType != ExpressionResultType.Boolean)
            {
                throw new ArgumentException("Ternary condition must be boolean expression");
            }

            if (ifTrue.ResultType == ExpressionResultType.Boolean || ifFalse.ResultType == ExpressionResultType.Boolean)
            {
                throw new ArgumentException("Ternary ifTrue and ifFalse must be non-boolean expression");
            }

            Condition = condition;
            IfTrue = ifTrue;
            IfFalse = ifFalse;
        }

        public override object Evaluate()
        {
            return (bool)Condition.Evaluate() ? (decimal)IfTrue.Evaluate() : (decimal)IfFalse.Evaluate();
        }
    }
}
