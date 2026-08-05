namespace Application.Features.EmpiricTreatmentProtocols.DeleteEmpiricTreatmentProtocol;

public class DeleteEmpiricTreatmentProtocolValidator : AbstractValidator<DeleteEmpiricTreatmentProtocolCommand>
{
    public DeleteEmpiricTreatmentProtocolValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Empiric treatment protocol ID is required");
    }
}