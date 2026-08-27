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
            .Must(x => x == null || x.Trim() != "")
            .WithMessage("Unit must not be an empty string");
    }
}
