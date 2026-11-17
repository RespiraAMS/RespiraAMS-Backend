using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.SearchClinicalMetric
{
    public class SearchClinicalMetricValidator : AbstractValidator<SearchClinicalMetricQuery>
    {
        public SearchClinicalMetricValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();
            RuleFor(x => x.SearchTerm).IsValidSearchTerm();
        }
    }
}
