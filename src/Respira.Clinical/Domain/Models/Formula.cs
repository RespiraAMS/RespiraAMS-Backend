using System.Globalization;
using System.Text.Json.Serialization;
using Respira.Clinical.Domain.Enums;

namespace Respira.Clinical.Domain.Models
{
    /// <summary>
    /// This is the class represent a clinical formula, like BUN, Systoloc blood pressure,...
    /// The structure of formula would be identical to <see cref="Expression"/>.
    /// This class will be stored in database as a complex value (e.g. JSONB), so this is
    /// not an entity
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(NumericConstantFormula), typeDiscriminator: "numeric")]
    [JsonDerivedType(typeof(BooleanConstantFormula), typeDiscriminator: "boolean")]
    [JsonDerivedType(typeof(CategoricalConstantFormula), typeDiscriminator: "categorical")]
    [JsonDerivedType(typeof(VariableFormula), typeDiscriminator: "variable")]
    [JsonDerivedType(typeof(UnaryFormula), typeDiscriminator: "unary")]
    [JsonDerivedType(typeof(BinaryFormula), typeDiscriminator: "binary")]
    [JsonDerivedType(typeof(TernaryFormula), typeDiscriminator: "ternary")]
    public abstract class Formula
    {
        /// <summary>
        /// The result type of the formula
        /// </summary>
        [JsonIgnore]
        public abstract ExpressionResultType ResultType { get; }

        /// <summary>
        /// Converts this stored formula into a runtime expression
        /// using actual clinical observations.
        /// </summary>
        public abstract Expression ToExpression(IEnumerable<ClinicalObservation> observations);

        /// <summary>
        /// The variables used by this formula. Derived, runtime-only data that must never
        /// be written into (or read from) the stored JSONB payload.
        /// </summary>
        [JsonIgnore]
        public abstract IEnumerable<VariableRef> Variables { get; }

        /// <summary>
        /// Renders a sub-formula, wrapping anything that is not a leaf (constant or
        /// variable) in parentheses so the mathematical representation stays unambiguous
        /// </summary>
        private protected static string Parenthesize(Formula formula)
        {
            return formula switch
            {
                NumericConstantFormula or BooleanConstantFormula or CategoricalConstantFormula or VariableFormula
                    => formula.ToString() ?? string.Empty,
                _ => $"({formula})",
            };
        }
    }

    /// <summary>
    /// This formula is used to represent a numeric constant
    /// </summary>
    /// <param name="constant">Numeric constant value</param>
    public class NumericConstantFormula(decimal constant) : Formula
    {
        // Need to store this for JSON serialization
        public decimal Constant { get; } = constant;

        [JsonIgnore]
        public override ExpressionResultType ResultType => ExpressionResultType.Numeric;

        [JsonIgnore]
        public override IEnumerable<VariableRef> Variables => [];

        public override Expression ToExpression(IEnumerable<ClinicalObservation> observations)
        {
            return new NumericalExpression(Constant);
        }

        public override string ToString()
        {
            return Constant.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// This formula is used to represent a boolean constant
    /// </summary>
    /// <param name="constant">Boolean constant value</param>
    public class BooleanConstantFormula(bool constant) : Formula
    {
        // Need to store this for JSON serialization
        public bool Constant { get; } = constant;
        [JsonIgnore]
        public override ExpressionResultType ResultType => ExpressionResultType.Boolean;

        [JsonIgnore]
        public override IEnumerable<VariableRef> Variables => [];

        public override Expression ToExpression(IEnumerable<ClinicalObservation> observations)
        {
            return new BooleanExpression(Constant);
        }

        public override string ToString()
        {
            return Constant ? "true" : "false";
        }
    }

    public class CategoricalConstantFormula(string constant) : Formula
    {
        // Need to store this for JSON serialization
        public string Constant { get; } = constant;

        [JsonIgnore]
        public override ExpressionResultType ResultType => ExpressionResultType.String;

        [JsonIgnore]
        public override IEnumerable<VariableRef> Variables => [];

        public override Expression ToExpression(IEnumerable<ClinicalObservation> observations)
        {
            return new CategoricalExpression(Constant);
        }

        public override string ToString()
        {
            return $"\"{Constant}\"";
        }
    }

    /// <summary>
    /// This formula is used to represent a clinical variable
    /// </summary>
    /// <param name="variable">Clinical variable</param>
    public class VariableFormula(VariableRef variable) : Formula
    {
        public VariableRef Variable { get; } = variable;

        [JsonIgnore]
        public override IEnumerable<VariableRef> Variables => [Variable];

        [JsonIgnore]
        public override ExpressionResultType ResultType => Variable.ValueType switch
        {
            ClinicalValueType.Boolean => ExpressionResultType.Boolean,
            ClinicalValueType.Numeric => ExpressionResultType.Numeric,
            ClinicalValueType.Categorical => ExpressionResultType.String,
            _ => throw new InvalidOperationException($"Unexpected clinical value type: {Variable.ValueType}")
        };
        public override Expression ToExpression(IEnumerable<ClinicalObservation> observations)
        {
            var observation = observations.SingleOrDefault(x => x.Variable.Code == Variable.Code);

            return observation is null
                ? throw new ArgumentException($"Observation not found for variable: {Variable.Code}")
                : (Expression)new ObservationExpression(observation);
        }

        public override string ToString()
        {
            return $"{Variable.Code}";
        }
    }

    /// <summary>
    /// This formula is used to represent a unary expression
    /// </summary>
    public class UnaryFormula : Formula
    {
        [JsonIgnore]
        public override ExpressionResultType ResultType => ExpressionResultType.Boolean;

        [JsonIgnore]
        public override IEnumerable<VariableRef> Variables => Formula.Variables;

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

        public override string ToString()
        {
            return ExpressionOperator.NOT.ToSymbol() + Parenthesize(Formula);
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

        [JsonIgnore]
        public override IEnumerable<VariableRef> Variables => Left.Variables.Concat(Right.Variables).DistinctBy(x => x.Code);

        [JsonIgnore]
        public override ExpressionResultType ResultType => Operator.IsBooleanResult()
            ? ExpressionResultType.Boolean
            : ExpressionResultType.Numeric;

        // Parameter must be named '@operator' so System.Text.Json can bind it to the
        // Operator property when deserializing (ctor params match property names).
        public BinaryFormula(Formula left, Formula right, ExpressionOperator @operator)
        {
            if (left.ResultType != right.ResultType)
            {
                throw new ArgumentException($"Operand type mismatch: (left) {left.ResultType} | (right) {right.ResultType}");
            }

            if (left.ResultType == ExpressionResultType.Boolean && !@operator.IsLogicalOperator() && @operator != ExpressionOperator.EQ && @operator != ExpressionOperator.NE)
            {
                throw new ArgumentException("Invalid expression: applying non-logical operator to boolean operands.");
            }

            if (left.ResultType == ExpressionResultType.Numeric && !@operator.IsMathematicalOperator())
            {
                throw new ArgumentException("Invalid expression: applying invalid operator to numeric operands.");
            }

            if (left.ResultType == ExpressionResultType.String && !(@operator == ExpressionOperator.EQ || @operator == ExpressionOperator.NE))
            {
                throw new ArgumentException("Invalid expression: applying non-string operator to string operands.");
            }

            Left = left;
            Right = right;
            Operator = @operator;
        }

        public override Expression ToExpression(IEnumerable<ClinicalObservation> observations)
        {
            // Handle the case for AND, OR operators
            // For AND, if one operand is false, the result is false
            // For OR, if one operand is true, the result is true
            // So, in some rare cases, even if the observations are not supplied enough,
            // the expression can still be evaluated

            if (Operator == ExpressionOperator.AND)
            {
                // Check on left
                try
                {
                    if (!(bool)Left.ToExpression(observations).Evaluate())
                    {
                        return new BooleanExpression(false);
                    }
                }
                catch (ArgumentException) { }

                // Check on right
                try
                {
                    if (!(bool)Right.ToExpression(observations).Evaluate())
                    {
                        return new BooleanExpression(false);
                    }
                }
                catch (ArgumentException) { }

                // If neither left nor right is false, then we simply fall to the default
                // case, which can either be evaluated (if all required observations are supplied) or
                // throw if not enough observations are supplied
            }

            if (Operator == ExpressionOperator.OR)
            {
                // Check on left
                try
                {
                    if ((bool)Left.ToExpression(observations).Evaluate())
                    {
                        return new BooleanExpression(true);
                    }
                }
                catch (ArgumentException) { }

                // Check on right
                try
                {
                    if ((bool)Right.ToExpression(observations).Evaluate())
                    {
                        return new BooleanExpression(true);
                    }
                }
                catch (ArgumentException) { }

                // Same cases with AND
            }

            return new BinaryExpression(Left.ToExpression(observations), Right.ToExpression(observations), Operator);
        }

        public override string ToString()
        {
            return $"{Parenthesize(Left)} {Operator.ToSymbol()} {Parenthesize(Right)}";
        }
    }

    /// <summary>
    /// This formula is used to represent a ternary expression
    /// </summary>
    public class TernaryFormula : Formula
    {
        [JsonIgnore]
        public override ExpressionResultType ResultType => IfTrue.ResultType;

        [JsonIgnore]
        public override IEnumerable<VariableRef> Variables => Condition.Variables.Concat(IfTrue.Variables).Concat(IfFalse.Variables).DistinctBy(x => x.Code);

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

            if (ifTrue.ResultType != ifFalse.ResultType)
            {
                throw new ArgumentException("Ternary ifTrue and ifFalse must have the same result type");
            }

            Condition = condition;
            IfTrue = ifTrue;
            IfFalse = ifFalse;
        }
        public override Expression ToExpression(IEnumerable<ClinicalObservation> observations)
        {
            // Same cases with binary expression AND or OR, ternary expression can also got a special
            // case: if condition true, only if true branch is required and vice versa.
            // So, we will just build a dump expression on the unreachable branch so that
            // the expression builder won't crash, while preserve the result since it's unreachable

            // Condition and reachable branch are required, so we don't use try catch here so that it can throw
            var condition = (bool)Condition.ToExpression(observations).Evaluate();
            return condition
                ? new TernaryExpression(Condition.ToExpression(observations), IfTrue.ToExpression(observations), IfTrue.ToExpression(observations))
                : new TernaryExpression(Condition.ToExpression(observations), IfFalse.ToExpression(observations), IfFalse.ToExpression(observations));
        }

        public override string ToString()
        {
            return $"If {Parenthesize(Condition)} then {Parenthesize(IfTrue)} else {Parenthesize(IfFalse)}";
        }
    }
}
