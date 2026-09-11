using Respira.Domain.Entities;
using Respira.Domain.Enums;

namespace Respira.Domain.Models
{
    /// <summary>
    /// This is the class represent a clinical formula, like BUN, Systoloc blood pressure,...
    /// The structure of formula would be identical to <see cref="Expression"/>.
    /// This class will be stored in database as a complex value (e.g. JSONB), so this is
    /// not an entity
    /// </summary>
    public abstract class Formula
    {
        /// <summary>
        /// The result type of the formula
        /// </summary>
        public abstract ExpressionResultType ResultType { get; }

        /// <summary>
        /// Converts this stored formula into a runtime expression
        /// using actual clinical observations.
        /// </summary>
        public abstract Expression ToExpression(IEnumerable<ClinicalObservation> observations);

        /// <summary>
        /// The variables used by this formula
        /// </summary>
        public abstract IEnumerable<ClinicalVariable> Variables { get; }
    }

    /// <summary>
    /// This formula is used to represent a numeric constant
    /// </summary>
    /// <param name="constant">Numeric constant value</param>
    public class NumericConstantFormula(decimal constant) : Formula
    {
        public override ExpressionResultType ResultType => ExpressionResultType.Numeric;

        public override IEnumerable<ClinicalVariable> Variables => [];

        public override Expression ToExpression(IEnumerable<ClinicalObservation> observations)
        {
            return new NumericalExpression(constant);
        }
    }

    /// <summary>
    /// This formula is used to represent a boolean constant
    /// </summary>
    /// <param name="constant">Boolean constant value</param>
    public class BooleanConstantFormula(bool constant) : Formula
    {
        public override ExpressionResultType ResultType => ExpressionResultType.Boolean;

        public override IEnumerable<ClinicalVariable> Variables => [];

        public override Expression ToExpression(IEnumerable<ClinicalObservation> observations)
        {
            return new BooleanExpression(constant);
        }
    }

    /// <summary>
    /// This formula is used to represent a clinical variable
    /// </summary>
    /// <param name="variable">Clinical variable</param>
    public class VariableFormula(ClinicalVariable variable) : Formula
    {
        public ClinicalVariable Variable { get; } = variable;

        public override IEnumerable<ClinicalVariable> Variables => [Variable];

        public override ExpressionResultType ResultType => Variable.ValueType == ClinicalValueType.Boolean
            ? ExpressionResultType.Boolean
            : ExpressionResultType.Numeric;

        public override Expression ToExpression(IEnumerable<ClinicalObservation> observations)
        {
            var observation = observations.SingleOrDefault(x => x.Variable.Code == Variable.Code);

            return observation is null
                ? throw new ArgumentException($"Observation not found for variable: {Variable.Code}")
                : (Expression)new ObservationExpression(observation);
        }
    }

    /// <summary>
    /// This formula is used to represent a unary expression
    /// </summary>
    public class UnaryFormula : Formula
    {
        public override ExpressionResultType ResultType => ExpressionResultType.Boolean;

        public override IEnumerable<ClinicalVariable> Variables => Formula.Variables;

        public Formula Formula { get; }

        public UnaryFormula(Formula formula)
        {
            if (formula.ResultType != ExpressionResultType.Boolean)
            {
                throw new ArgumentException("Unary formula only support boolean formula");
            }

            Formula = formula;
        }

        public override Expression ToExpression(IEnumerable<ClinicalObservation> observations)
        {
            return new UnaryExpression(Formula.ToExpression(observations));
        }
    }

    /// <summary>
    /// This formula is used to represent a binary expression
    /// </summary>
    public class BinaryFormula : Formula
    {
        /// <summary>
        /// Left operand
        /// </summary>
        public Formula Left { get; }

        /// <summary>
        /// Right operand
        /// </summary>
        public Formula Right { get; }

        /// <summary>
        /// Operator
        /// </summary>
        public ExpressionOperator Operator { get; }

        public override IEnumerable<ClinicalVariable> Variables => Left.Variables.Concat(Right.Variables).DistinctBy(x => x.Code);

        public override ExpressionResultType ResultType => Operator.IsBooleanResult()
            ? ExpressionResultType.Boolean
            : ExpressionResultType.Numeric;

        public BinaryFormula(Formula left, Formula right, ExpressionOperator op)
        {
            if (left.ResultType != right.ResultType)
            {
                throw new ArgumentException($"Operand type mismatch: (left) {left.ResultType} | (right) {right.ResultType}");
            }

            if (left.ResultType == ExpressionResultType.Boolean && !op.IsLogicalOperator() && op != ExpressionOperator.EQ && op != ExpressionOperator.NE)
            {
                throw new ArgumentException("Invalid expression: applying non-logical operator to boolean operands.");
            }

            if (left.ResultType == ExpressionResultType.Numeric && !op.IsMathematicalOperator())
            {
                throw new ArgumentException("Invalid expression: applying invalid operator to numeric operands.");
            }

            Left = left;
            Right = right;
            Operator = op;
        }

        public override Expression ToExpression(IEnumerable<ClinicalObservation> observations)
        {
            return new BinaryExpression(Left.ToExpression(observations), Right.ToExpression(observations), Operator);
        }
    }

    /// <summary>
    /// This formula is used to represent a ternary expression
    /// </summary>
    public class TernaryFormula : Formula
    {
        public override ExpressionResultType ResultType => ExpressionResultType.Boolean;

        public override IEnumerable<ClinicalVariable> Variables => Condition.Variables.Concat(IfTrue.Variables).Concat(IfFalse.Variables).DistinctBy(x => x.Code);

        /// <summary>
        /// Condition formula
        /// </summary>
        public Formula Condition { get; }

        /// <summary>
        /// If true formula
        /// </summary>
        public Formula IfTrue { get; }

        /// <summary>
        /// If false formula
        /// </summary>
        public Formula IfFalse { get; }

        public TernaryFormula(Formula condition, Formula ifTrue, Formula ifFalse)
        {
            if (condition.ResultType != ExpressionResultType.Boolean)
            {
                throw new ArgumentException("Ternary condition must be boolean expression");
            }

            if (ifTrue.ResultType == ExpressionResultType.Boolean && ifFalse.ResultType == ExpressionResultType.Boolean)
            {
                throw new ArgumentException("Ternary ifTrue and ifFalse must be non-boolean expression");
            }

            Condition = condition;
            IfTrue = ifTrue;
            IfFalse = ifFalse;
        }
        public override Expression ToExpression(IEnumerable<ClinicalObservation> observations)
        {
            return new TernaryExpression(Condition.ToExpression(observations), IfTrue.ToExpression(observations), IfFalse.ToExpression(observations));
        }
    }
}
