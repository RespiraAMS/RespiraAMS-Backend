namespace Application.Features.Antibiotics.GetAntibioticById;

public class GetAntibioticByIdValidator : AbstractValidator<GetAntibioticByIdQuery>
{
    public GetAntibioticByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Antibiotic ID is required");
    }
}