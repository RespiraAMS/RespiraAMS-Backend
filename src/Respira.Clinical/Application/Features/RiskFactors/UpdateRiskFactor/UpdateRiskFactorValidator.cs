using FluentValidation;

namespace Respira.Clinical.Application.Features.RiskFactors.UpdateRiskFactor
{
    public class UpdateRiskFactorValidator : AbstractValidator<UpdateRiskFactorCommand>
    {
        public UpdateRiskFactorValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Risk factor ID is required");
            RuleFor(x => x.PathogenId)
                .NotEmpty()
                .WithMessage("Pathogen ID is required");
            RuleFor(x => x.CriterionId)
                .NotEmpty()
                .WithMessage("Criterion ID is required");
        }
    }
}
