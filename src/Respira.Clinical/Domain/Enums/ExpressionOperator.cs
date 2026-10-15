using System.Text.Json.Serialization;

namespace Respira.Clinical.Domain.Enums
{
    /// <summary>
    /// Expression operator
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExpressionOperator
    {
        Add,
        Sub,
        Mul,
        Div,
        Gt,
        Lt,
        Gte,
        Lte,
        Eq,
        Ne,
        And,
        Or,
        Not,
        Ternary
    }

    /// <summary>
    /// Extension methods for <see cref="ExpressionOperator"/>
    /// </summary>
    public static class ExpressionOperatorExtensions
    {
        /// <summary>
        /// Check if the operator is a mathematical operator
        /// </summary>
        /// <param name="op">Expression operator</param>
        /// <returns>True if mathematical, false otherwise</returns>
        public static bool IsMathematicalOperator(this ExpressionOperator op)
        {
            return op switch
            {
                ExpressionOperator.Add => true,
                ExpressionOperator.Sub => true,
                ExpressionOperator.Mul => true,
                ExpressionOperator.Div => true,
                ExpressionOperator.Gt => true,
                ExpressionOperator.Lt => true,
                ExpressionOperator.Gte => true,
                ExpressionOperator.Lte => true,
                ExpressionOperator.Eq => true,
                ExpressionOperator.Ne => true,
                _ => false,
            };
        }

        /// <summary>
        /// Check if the operator is a logical operator
        /// </summary>
        /// <param name="op">Expression operator</param>
        /// <returns>True if logical, false otherwise</returns>
        public static bool IsLogicalOperator(this ExpressionOperator op)
        {
            return op switch
            {
                ExpressionOperator.And => true,
                ExpressionOperator.Or => true,
                ExpressionOperator.Not => true,
                ExpressionOperator.Ternary => true,
                _ => false,
            };
        }

        /// <summary>
        /// Check if the operator is a boolean result operator
        /// </summary>
        /// <param name="op">Expression operator</param>
        /// <returns>True if boolean result, false otherwise</returns>
        public static bool IsBooleanResult(this ExpressionOperator op)
        {
            return op switch
            {
                ExpressionOperator.And => true,
                ExpressionOperator.Or => true,
                ExpressionOperator.Not => true,
                ExpressionOperator.Eq => true,
                ExpressionOperator.Ne => true,
                ExpressionOperator.Gt => true,
                ExpressionOperator.Lt => true,
                ExpressionOperator.Gte => true,
                ExpressionOperator.Lte => true,
                // Technically, Ternary operator can still return a boolean result,
                // but a ternary operation already required a boolean expression as
                // condition, so return boolean with ternary is somewhat redundant,
                // so we will strictly forbid it
                _ => false,
            };
        }

        /// <summary>
        /// Renders an operator as its mathematical symbol, for example GTE as ≥.
        /// Note that this is an extension method (and not an override), so it must be
        /// called as <c>op.ToSymbol()</c> - a plain <c>op.ToString()</c> would resolve
        /// to the enum name instead
        /// </summary>
        public static string ToSymbol(this ExpressionOperator op)
        {
            return op switch
            {
                ExpressionOperator.Add => "+",
                ExpressionOperator.Sub => "−",
                ExpressionOperator.Mul => "×",
                ExpressionOperator.Div => "÷",
                ExpressionOperator.Gt => ">",
                ExpressionOperator.Lt => "<",
                ExpressionOperator.Gte => "≥",
                ExpressionOperator.Lte => "≤",
                ExpressionOperator.Eq => "=",
                ExpressionOperator.Ne => "≠",
                ExpressionOperator.And => "AND",
                ExpressionOperator.Or => "OR",
                ExpressionOperator.Not => "NOT",
                _ => op.ToString(),
            };
        }
    }
}
