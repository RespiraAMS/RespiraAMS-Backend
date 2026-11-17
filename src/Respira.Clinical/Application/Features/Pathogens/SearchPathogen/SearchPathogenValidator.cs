using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Clinical.Application.Features.Pathogens.SearchPathogen
{
    public class SearchPathogenValidator : AbstractValidator<SearchPathogenQuery>
    {
        public SearchPathogenValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();
            RuleFor(x => x.SearchTerm).IsValidSearchTerm();
        }
    }
}
