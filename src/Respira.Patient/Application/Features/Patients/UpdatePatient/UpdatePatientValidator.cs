using FluentValidation;
using Respira.Patient.Domain.Utils;

namespace Respira.Patient.Application.Features.Patients.UpdatePatient
{
    public class UpdatePatientValidator : AbstractValidator<UpdatePatientCommand>
    {
        public UpdatePatientValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Patient ID is required");
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("Patient first name is required");
            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Patient last name is required");
            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("Patient phone number is required");
            RuleFor(x => x.Gender)
                .IsInEnum()
                .WithMessage("Patient gender is required");
            RuleFor(x => x.Height)
                .GreaterThan(0)
                .WithMessage("Patient height must be greater than 0");
            RuleFor(x => x.Weight)
                .GreaterThan(0)
                .WithMessage("Patient weight must be greater than 0");
            RuleFor(x => x.DateOfBirth)
                .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("Patient date of birth must not be in future");
            RuleFor(x => x.MedicalRecordCode)
                .NotEmpty()
                .WithMessage("Patient medical record code is required");
            RuleFor(x => x.HealthInsuranceCard)
                .Must(HealthInsuranceCardValidator.IsValid)
                .WithMessage("Patient health insurance card number is required, and must be a 10 numeric characters string");
            RuleFor(x => x.Address)
                .NotEmpty()
                .WithMessage("Patient address is required");
            RuleFor(x => x.City)
                .NotEmpty()
                .WithMessage("Patient city is required");
        }
    }
}
