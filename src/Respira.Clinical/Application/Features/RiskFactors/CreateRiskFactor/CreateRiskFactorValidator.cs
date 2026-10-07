using FluentValidation;

namespace Respira.Clinical.Application.Features.RiskFactors.CreateRiskFactor
{
    public class CreateRiskFactorValidator : AbstractValidator<CreateRiskFactorCommand>
    {
        public CreateRiskFactorValidator()
        {
            RuleFor(x => x.PathogenId)
                .NotEmpty()
                .WithMessage("Pathogen ID is required");
            RuleFor(x => x.CriterionId)
                .NotEmpty()
                .WithMessage("Criterion ID is required");
        }
    }
}
