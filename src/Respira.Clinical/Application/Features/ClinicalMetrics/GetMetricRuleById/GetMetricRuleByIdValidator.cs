using FluentValidation;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.GetMetricRuleById
{
    public class GetMetricRuleByIdValidator : AbstractValidator<GetMetricRuleByIdQuery>
    {
        public GetMetricRuleByIdValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Metric rule ID is required");
        }
    }
}
