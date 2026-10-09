using FluentValidation;
using Respira.Clinical.Application.Features.Shared.ManageFormula;

namespace Respira.Clinical.Application.Features.Criteria.CreateCriterion
{
    public class CreateCriterionValidator : AbstractValidator<CreateCriterionCommand>
    {
        public CreateCriterionValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Criterion name must not be empty");
            RuleFor(x => x.Formula)
                .SetValidator(new FormulaValidator());
        }
    }
}
