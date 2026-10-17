using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Clinical.Application.Features.AntibioticGroups.SearchAntibioticGroup
{
    public class SearchAntibioticGroupValidator : AbstractValidator<SearchAntibioticGroupQuery>
    {
        public SearchAntibioticGroupValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();
            RuleFor(x => x.SearchTerm).IsValidSearchTerm();
        }
    }
}
