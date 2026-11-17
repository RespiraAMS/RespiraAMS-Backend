using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Clinical.Application.Features.Criteria.GetPagedCriterion
{
    public class GetPagedCriterionValidator : AbstractValidator<GetPagedCriterionQuery>
    {
        public GetPagedCriterionValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();
        }
    }
}
