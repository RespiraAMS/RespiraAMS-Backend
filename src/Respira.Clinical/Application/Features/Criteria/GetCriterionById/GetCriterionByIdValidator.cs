using FluentValidation;

namespace Respira.Clinical.Application.Features.Criteria.GetCriterionById
{
    public class GetCriterionByIdValidator : AbstractValidator<GetCriterionByIdQuery>
    {
        public GetCriterionByIdValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Criterion ID must be provided");
        }
    }
}
