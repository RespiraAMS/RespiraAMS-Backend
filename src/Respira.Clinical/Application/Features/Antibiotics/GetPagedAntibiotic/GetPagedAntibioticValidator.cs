using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Clinical.Application.Features.Antibiotics.GetPagedAntibiotic
{
    public class GetPagedAntibioticValidator : AbstractValidator<GetPagedAntibioticQuery>
    {
        public GetPagedAntibioticValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();
        }
    }
}
