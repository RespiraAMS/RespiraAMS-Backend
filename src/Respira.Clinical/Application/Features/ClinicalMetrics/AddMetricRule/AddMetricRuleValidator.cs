using FluentValidation;
using Respira.Clinical.Application.Features.Shared.ManageFormula;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.AddMetricRule
{
    public class AddMetricRuleValidator : AbstractValidator<AddMetricRuleCommand>
    {
        public AddMetricRuleValidator()
        {
            RuleFor(x => x.ClinicalMetricId).NotEmpty().WithMessage("Clinical metric ID is required");
            RuleFor(x => x.CriterionId).NotEmpty().WithMessage("Criterion ID is required");
            RuleFor(x => x)
                .Must(x => (x.ScoreFunction is null && x.IsMajor is not null) ||
                        (x.ScoreFunction is not null && x.IsMajor is null))
                .WithMessage("Only one of score function or major/minor can be set");
            RuleFor(x => x.ScoreFunction)
                .SetValidator(new FormulaValidator()!)
                .When(x => x.ScoreFunction is not null);
        }
    }
}
