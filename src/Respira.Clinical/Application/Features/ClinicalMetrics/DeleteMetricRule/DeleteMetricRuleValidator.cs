using FluentValidation;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.DeleteMetricRule
{
    public class DeleteMetricRuleValidator : AbstractValidator<DeleteMetricRuleCommand>
    {
        public DeleteMetricRuleValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Metric rule ID is required");
        }
    }
}
