using Application.Features.IcuHospitalizeCriteria.UpdateIcuHospitalizeCriterion;
using Application.Features.Shared.ManageCriterion;

namespace Application.Features.IcuHospitalizeCriteria.UpdateIcuHospitalizeCriterion;

public class UpdateIcuHospitalizeCriterionValidator : AbstractValidator<UpdateIcuHospitalizeCriterionCommand>
{
    public UpdateIcuHospitalizeCriterionValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Disease's ICU hospitalize criterion ID is required");
        RuleFor(x => x.Criterion)
            .SetValidator(new UpdateCriterionValidator());
        RuleFor(x => x.Score)
            .GreaterThan(0)
            .WithMessage("Score must be a positive integer");
    }
}