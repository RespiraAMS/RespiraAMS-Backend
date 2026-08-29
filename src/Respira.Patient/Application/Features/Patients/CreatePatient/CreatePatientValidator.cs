namespace Application.Features.Patients.CreatePatient;

public class CreatePatientValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientValidator()
    {
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
        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("Patient city is required");
        RuleFor(x => x.Country)
            .NotEmpty()
            .WithMessage("Patient country is required");

    }
}
