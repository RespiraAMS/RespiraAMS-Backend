using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Clinical.Application.Features.ClinicalVariables.SearchClinicalVariable
{
    public class SearchClinicalVariableValidator : AbstractValidator<SearchClinicalVariableQuery>
    {
        public SearchClinicalVariableValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();
            RuleFor(x => x.SearchTerm).IsValidSearchTerm();
        }
    }
}
