namespace Application.Features.ResistanceRiskFactors.DeleteResistanceRiskFactor;

public class DeleteResistanceRiskFactorValidator : AbstractValidator<DeleteResistanceRiskFactorCommand>
{
    public DeleteResistanceRiskFactorValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("Resistance risk factor is required");
    }
}