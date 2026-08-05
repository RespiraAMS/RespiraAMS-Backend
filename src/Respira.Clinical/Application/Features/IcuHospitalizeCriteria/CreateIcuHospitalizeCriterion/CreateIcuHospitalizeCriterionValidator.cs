using Application.Features.Shared.ManageCriterion;

namespace Application.Features.IcuHospitalizeCriteria.CreateIcuHospitalizeCriterion;

public class CreateIcuHospitalizeCriterionValidator : AbstractValidator<CreateIcuHospitalizeCriterionCommand>
{
    public CreateIcuHospitalizeCriterionValidator()
    {
        RuleFor(x => x.DiseaseId)
            .NotEmpty()
            .WithMessage("Disease ID is required");
        RuleFor(x => x.Criterion)
            .SetValidator(new CreateCriterionValidator());
        RuleFor(x => x.Score)
            .GreaterThan(0)
            .WithMessage("Score must be a positive integer");
    }
}