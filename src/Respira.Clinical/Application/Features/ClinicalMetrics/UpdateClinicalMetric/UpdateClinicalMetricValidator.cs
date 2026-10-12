using FluentValidation;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.UpdateClinicalMetric
{
    public class UpdateClinicalMetricValidator : AbstractValidator<UpdateClinicalMetricCommand>
    {
        public UpdateClinicalMetricValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Clinical metric id is required");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Clinical metric name is required");
            RuleFor(x => x.Code).NotEmpty().WithMessage("Clinical metric code is required");
            RuleFor(x => x.Description).NotEmpty().WithMessage("Clinical metric description is required");
            RuleFor(x => x.Version).NotEmpty().WithMessage("Clinical metric version is required");
            RuleFor(x => x.Publisher).NotEmpty().WithMessage("Clinical metric publisher is required");
        }
    }
}
