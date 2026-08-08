namespace Application.Features.Patients.UpdatePatient;

public class UpdatePatientValidator : AbstractValidator<UpdatePatientCommand>
{
    public UpdatePatientValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Patient ID is required");
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Patient fullname is required");
        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Patient date of birth must not be in future");
        RuleFor(x => x.MedicalRecordCode)
            .NotEmpty()
            .WithMessage("Patient medical record code is required");
        // New health insurance card number is 10 characters, while old one is 15
        RuleFor(x => x.HealthInsuranceCardNumber)
            .NotEmpty()
            .Length(10)
            .WithMessage("Patient health insurance card number is required");
        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Patient address is required");
    }
}