namespace Application.Features.EmpiricTreatmentProtocols.GetEmpiricTreatmentProtocolById;

public class GetEmpiricTreatmentProtocolByIdValidator : AbstractValidator<GetEmpiricTreatmentProtocolByIdQuery>
{
    public GetEmpiricTreatmentProtocolByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Empiric treatment protocol ID is required");
    }
}