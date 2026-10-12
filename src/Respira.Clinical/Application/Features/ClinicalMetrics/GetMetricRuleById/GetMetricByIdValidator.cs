using FluentValidation;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.GetMetricRuleById
{
    public class GetMetricByIdValidator : AbstractValidator<GetMetricRuleByIdQuery>
    {
        public GetMetricByIdValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Metric rule ID is required");
        }
    }
}
