namespace Application.Features.Patients.GetPatientById;

public class GetPatientByIdValidator : AbstractValidator<GetPatientByIdQuery>
{
    public GetPatientByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Patient ID is required");
    }
}