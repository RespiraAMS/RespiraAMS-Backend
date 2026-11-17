using FluentValidation;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.DeleteClinicalMetric
{
    public class DeleteClinicalMetricValidator : AbstractValidator<DeleteClinicalMetricCommand>
    {
        public DeleteClinicalMetricValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Clinical metric id is required");
        }
    }
}
