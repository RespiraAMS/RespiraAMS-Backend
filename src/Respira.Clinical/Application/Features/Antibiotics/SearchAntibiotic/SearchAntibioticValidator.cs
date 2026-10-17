using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Clinical.Application.Features.Antibiotics.SearchAntibiotic
{
    public class SearchAntibioticValidator : AbstractValidator<SearchAntibioticQuery>
    {
        public SearchAntibioticValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();
            RuleFor(x => x.SearchTerm).IsValidSearchTerm();
        }
    }
}
