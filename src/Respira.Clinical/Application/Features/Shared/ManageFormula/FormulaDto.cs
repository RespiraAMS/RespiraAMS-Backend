using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;

namespace Respira.Clinical.Application.Features.Shared.ManageFormula
{
    public record FormulaDto
    {
        /// <summary>
        /// Formula result type
        /// </summary>
        public ExpressionResultType ResultType { get; set; }

        /// <summary>
        /// Constant value, provided if this is a constant formula.
        /// When provided, it will be parsed into the appropriate type,
        /// so make sure to provide a valid value.
        /// </summary>
        public string? Constant { get; init; }

        /// <summary>
        /// <see cref="ClinicalVariable"/> ID. Provided if this is variable formula.
        /// </summary>
        public Guid? Variable { get; init; }

        /// <summary>
        /// Left operand. Provided if this is binary formula.
        /// </summary>
        public FormulaDto? Left { get; init; }

        /// <summary>
        /// Right operand. Provided if this is binary formula.
        /// </summary>
        public FormulaDto? Right { get; init; }

        /// <summary>
        /// Operator. Provided if this is binary formula.
        /// </summary>
        public ExpressionOperator? Operator { get; init; }

        /// <summary>
        /// Unary operand. Provided if this is unary formula.
        /// </summary>
        public FormulaDto? Operand { get; init; }

        /// <summary>
        /// Condition formula. Provided if this is ternary formula.
        /// </summary>
        public FormulaDto? Condition { get; init; }

        /// <summary>
        /// If true formula. Provided if this is ternary formula.
        /// </summary>
        public FormulaDto? IfTrue { get; init; }

        /// <summary>
        /// If false formula. Provided if this is ternary formula.
        /// </summary>
        public FormulaDto? IfFalse { get; init; }
    }
}
