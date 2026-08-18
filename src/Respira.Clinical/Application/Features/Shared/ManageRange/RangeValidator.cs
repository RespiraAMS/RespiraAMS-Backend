using Range = Domain.Models.Range;

namespace Application.Features.Shared.ManageRange;

public class RangeValidator : AbstractValidator<Range>
{
    public RangeValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Min <= x.Max)
            .WithMessage("Min must be less than or equal to max");
        RuleFor(x => x.Unit)
            .NotEmpty()
            .WithMessage("Unit must not be empty")
            .When(x => !string.IsNullOrEmpty(x.Unit));
    }
}
