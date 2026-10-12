using FluentValidation;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.CreateClinicalMetric
{
    public class CreateClinicalMetricValidator : AbstractValidator<CreateClinicalMetricCommand>
    {
        public CreateClinicalMetricValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Clinical metric name is required");
            RuleFor(x => x.Code).NotEmpty().WithMessage("Clinical metric code is required");
            RuleFor(x => x.Description).NotEmpty().WithMessage("Clinical metric description is required");
            RuleFor(x => x.Version).NotEmpty().WithMessage("Clinical metric version is required");
            RuleFor(x => x.Publisher).NotEmpty().WithMessage("Clinical metric publisher is required");
        }
    }
}
