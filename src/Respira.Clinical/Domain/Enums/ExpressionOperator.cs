namespace Respira.Domain.Enums
{
    /// <summary>
    /// Expression operator
    /// </summary>
    public enum ExpressionOperator
    {
        ADD,
        SUB,
        MUL,
        DIV,
        GT,
        LT,
        GTE,
        LTE,
        EQ,
        NE,
        AND,
        OR,
        NOT,
        TERNARY
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
                ExpressionOperator.ADD => true,
                ExpressionOperator.SUB => true,
                ExpressionOperator.MUL => true,
                ExpressionOperator.DIV => true,
                ExpressionOperator.GT => true,
                ExpressionOperator.LT => true,
                ExpressionOperator.GTE => true,
                ExpressionOperator.LTE => true,
                ExpressionOperator.EQ => true,
                ExpressionOperator.NE => true,
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
                ExpressionOperator.AND => true,
                ExpressionOperator.OR => true,
                ExpressionOperator.NOT => true,
                ExpressionOperator.TERNARY => true,
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
                ExpressionOperator.AND => true,
                ExpressionOperator.OR => true,
                ExpressionOperator.NOT => true,
                ExpressionOperator.EQ => true,
                ExpressionOperator.NE => true,
                ExpressionOperator.GT => true,
                ExpressionOperator.LT => true,
                ExpressionOperator.GTE => true,
                ExpressionOperator.LTE => true,
                // Technically, Ternary operator can still return a boolean result,
                // but a ternary operation already required a boolean expression as
                // condition, so return boolean with ternary is somewhat redundant,
                // so we will strictly forbid it
                _ => false,
            };
        }
    }
}
