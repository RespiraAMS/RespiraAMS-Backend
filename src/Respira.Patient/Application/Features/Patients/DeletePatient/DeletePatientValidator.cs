namespace Application.Features.Patients.DeletePatient;

public class DeletePatientValidator : AbstractValidator<DeletePatientCommand>
{
    public DeletePatientValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Patient ID is required");
    }
}