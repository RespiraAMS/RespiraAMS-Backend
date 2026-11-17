using FluentValidation;

namespace Respira.Clinical.Application.Features.RiskFactors.GetRiskFactorById
{
    public class GetRiskFactorByIdValidator : AbstractValidator<GetRiskFactorByIdQuery>
    {
        public GetRiskFactorByIdValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Risk factor ID is required");
        }
    }
}
