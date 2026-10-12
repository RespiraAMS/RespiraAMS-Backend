using FluentValidation;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.DeleteRule
{
    public class DeleteRuleValidator : AbstractValidator<DeleteRuleCommand>
    {
        public DeleteRuleValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Metric rule ID is required");
        }
    }
}
