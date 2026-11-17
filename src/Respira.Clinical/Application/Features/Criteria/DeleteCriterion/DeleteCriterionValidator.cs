using FluentValidation;

namespace Respira.Clinical.Application.Features.Criteria.DeleteCriterion
{
    public class DeleteCriterionValidator : AbstractValidator<DeleteCriterionCommand>
    {
        public DeleteCriterionValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Criterion ID is required");
        }
    }
}
