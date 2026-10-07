using FluentValidation;

namespace Respira.Clinical.Application.Features.Shared.ManageFormula
{
    public class FormulaValidator : AbstractValidator<FormulaDto>
    {
        public FormulaValidator()
        {
            RuleFor(x => x.ResultType)
                .IsInEnum()
                .WithMessage("Invalid value for formula result type");
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
