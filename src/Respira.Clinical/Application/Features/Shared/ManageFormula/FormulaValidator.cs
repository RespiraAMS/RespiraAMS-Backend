using FluentValidation;

namespace Respira.Clinical.Application.Features.Shared.ManageFormula
{
    public class FormulaValidator : AbstractValidator<FormulaDto>
    {
        public FormulaValidator()
        {
            // At least one field must present
            RuleFor(x => x)
                .Must(x =>
                        x.Constant is not null ||
                        x.Variable is not null ||
                        x.Operator is not null ||
                        x.Left is not null ||
                        x.Right is not null ||
                        x.Operand is not null ||
                        x.Condition is not null ||
                        x.IfTrue is not null ||
                        x.IfFalse is not null)
                .WithMessage("Formula cannot be empty");
            // If constant is supplied, other must be null
            RuleFor(x => x)
                .Must(x =>
                        x.Constant is not null &&
                        x.Variable is null &&
                        x.Operator is null &&
                        x.Left is null &&
                        x.Right is null &&
                        x.Operand is null &&
                        x.Condition is null &&
                        x.IfTrue is null &&
                        x.IfFalse is null)
                .WithMessage("Formula must be valid: constant formula must have constant not null and other fields are null")
                .When(x => x.Constant is not null);
            // If variable is supplied, other must be null
            RuleFor(x => x)
                .Must(x =>
                        x.Constant is null &&
                        x.Variable is not null &&
                        x.Operator is null &&
                        x.Left is null &&
                        x.Right is null &&
                        x.Operand is null &&
                        x.Condition is null &&
                        x.IfTrue is null &&
                        x.IfFalse is null)
                .WithMessage("Formula must be valid: if variable is supplied, other must be null")
                .When(x => x.Variable is not null);
            // If left is supplied, then right and operator must be present,
            // while others must be null
            RuleFor(x => x)
                .Must(x =>
                        x.Constant is null &&
                        x.Variable is null &&
                        x.Left is not null &&
                        x.Right is not null &&
                        x.Operator is not null &&
                        x.Operand is null &&
                        x.Condition is null &&
                        x.IfTrue is null &&
                        x.IfFalse is null)
                .WithMessage("Formula must be valid: if binary formula, then left, right and operator must not null while others must be null")
                .When(x => x.Left is not null || x.Right is not null || x.Operator is not null);
            // If operand is supplied, other must be null
            RuleFor(x => x)
                .Must(x =>
                        x.Constant is null &&
                        x.Variable is null &&
                        x.Left is null &&
                        x.Right is null &&
                        x.Operator is null &&
                        x.Operand is not null &&
                        x.Condition is null &&
                        x.IfTrue is null &&
                        x.IfFalse is null)
                .WithMessage("Formula must be valid: if unary formula, then operand must not null while others must be null")
                .When(x => x.Operand is not null);
            // If condition is supplied, then ifTrue and ifFalse must be present,
            // while others must be null
            RuleFor(x => x)
                .Must(x =>
                        x.Constant is null &&
                        x.Variable is null &&
                        x.Left is null &&
                        x.Right is null &&
                        x.Operator is null &&
                        x.Operand is null &&
                        x.Condition is not null &&
                        x.IfTrue is not null &&
                        x.IfFalse is not null)
                .WithMessage("Formula must be valid: if ternary formula, then condition, ifTrue and ifFalse must not null while others must be null")
                .When(x => x.Condition is not null || x.IfTrue is not null || x.IfFalse is not null);
            RuleFor(x => x.Constant)
                .Must(x => x is null || x != string.Empty)
                .WithMessage("Constant must be a non-empty string");
            RuleFor(x => x.Variable)
                .Must(x => x is null || x != Guid.Empty)
                .WithMessage("Variable must be a valid guid");
            RuleFor(x => x.Left)
                .SetValidator(this!)
                .When(x => x is not null);
            RuleFor(x => x.Right)
                .SetValidator(this!)
                .When(x => x is not null);
            RuleFor(x => x.Operand)
                .SetValidator(this!)
                .When(x => x is not null);
            RuleFor(x => x.Operator)
                .IsInEnum()
                .When(x => x is not null)
                .WithMessage("Operator must either be null or valid value");
            RuleFor(x => x.Condition)
                .SetValidator(this!)
                .When(x => x is not null);
            RuleFor(x => x.IfTrue)
                .SetValidator(this!)
                .When(x => x is not null);
            RuleFor(x => x.IfFalse)
                .SetValidator(this!)
                .When(x => x is not null);
        }
    }
}
