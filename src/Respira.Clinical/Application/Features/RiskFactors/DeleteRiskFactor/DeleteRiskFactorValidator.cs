using FluentValidation;

namespace Respira.Clinical.Application.Features.RiskFactors.DeleteRiskFactor
{
    public class DeleteRiskFactorValidator : AbstractValidator<DeleteRiskFactorCommand>
    {
        public DeleteRiskFactorValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Risk factor ID is required");
        }
    }
}
