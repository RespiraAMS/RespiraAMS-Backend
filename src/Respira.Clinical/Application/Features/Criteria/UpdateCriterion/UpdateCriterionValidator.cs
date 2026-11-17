using FluentValidation;
using Respira.Clinical.Application.Features.Shared.ManageFormula;

namespace Respira.Clinical.Application.Features.Criteria.UpdateCriterion
{
    public class UpdateCriterionValidator : AbstractValidator<UpdateCriterionCommand>
    {
        public UpdateCriterionValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Criterion ID is required");
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Criterion name must not be empty");
            RuleFor(x => x.Formula)
                .SetValidator(new FormulaValidator());
        }
    }
}
