using Application.Features.Shared.ManageRange;
using Domain.Enums;

namespace Application.Features.Shared.ManageCriterion;

public class CreateCriterionValidator : AbstractValidator<CreateCriterionCommand>
{
    public CreateCriterionValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Criterion name is required");
        RuleFor(c => c.Type)
            .IsInEnum().WithMessage("Invalid value for criterion type");

        // When type is numeric, the specific properties of NumericCriterion 
        // must exist and valid
        When(x => x.Type == CriterionType.Numeric, () =>
        {
            RuleFor(x => x.Value)
                .NotNull()
                .WithMessage("Criterion type is set to numeric, Value is required");
            RuleFor(x => x.Value)
                .SetValidator(new RangeValidator()!);
        });

        // When type is Boolean, all NumericCriterion properties must not exist
        When(x => x.Type == CriterionType.Boolean, () =>
        {
            RuleFor(x => x.Value)
                .Null()
                .WithMessage("Criterion type is set to boolean, Value must be null");
        });
    }
}

public class UpdateCriterionValidator : AbstractValidator<UpdateCriterionCommand>
{
    public UpdateCriterionValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Criterion name is required");
        When(x => x.Type == CriterionType.Numeric, () =>
        {
            RuleFor(x => x.Value)
                .NotNull()
                .WithMessage("Criterion type is set to numeric, Value is required");
            RuleFor(x => x.Value)
                .SetValidator(new RangeValidator()!);
        });
        When(x => x.Type == CriterionType.Boolean, () =>
        {
            RuleFor(x => x.Value)
                .Null()
                .WithMessage("Criterion type is set to boolean, Value must be null");
        });
    }
}