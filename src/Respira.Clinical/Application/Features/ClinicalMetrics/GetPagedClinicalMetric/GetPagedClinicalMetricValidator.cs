using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.GetPagedClinicalMetric
{
    public class GetPagedClinicalMetricValidator : AbstractValidator<GetPagedClinicalMetricQuery>
    {
        public GetPagedClinicalMetricValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();
        }
    }
}
