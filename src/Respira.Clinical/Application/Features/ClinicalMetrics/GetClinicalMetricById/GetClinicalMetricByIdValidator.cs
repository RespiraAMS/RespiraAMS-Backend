using FluentValidation;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.GetClinicalMetricById
{
    public class GetClinicalMetricByIdValidator : AbstractValidator<GetClinicalMetricByIdQuery>
    {
        public GetClinicalMetricByIdValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Clinical metric ID is required");
        }
    }
}
