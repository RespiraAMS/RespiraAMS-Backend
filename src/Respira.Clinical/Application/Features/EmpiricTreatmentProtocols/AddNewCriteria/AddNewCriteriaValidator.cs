using Application.Features.Shared.ManageCriterion;

namespace Application.Features.EmpiricTreatmentProtocols.AddNewCriteria;

public class AddNewCriteriaValidator : AbstractValidator<AddNewCriteriaCommand>
{
    public AddNewCriteriaValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Treatment protocol ID is required");
        RuleFor(x => x.Criteria)
            .NotEmpty()
            .WithMessage("Criteria items must not be empty");
        RuleForEach(x => x.Criteria)
            .SetValidator(new CreateCriterionValidator());
    }
}