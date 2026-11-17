using FluentValidation;
using Respira.Patient.Domain.Utils;

namespace Respira.Patient.Application.Features.Patients.AddNextOfKin
{
    public class AddNextOfKinValidator : AbstractValidator<AddNextOfKinCommand>
    {
        public AddNextOfKinValidator()
        {
            RuleFor(x => x.PatientId).NotEmpty().WithMessage("Patient ID is required");
            RuleFor(x => x.FullName).NotEmpty().WithMessage("Next of kin full name is required");
            RuleFor(x => x.PhoneNumber)
                .Must(PhoneNumberValidator.IsValid)
                .WithMessage("Next of kin phone number is required, and must match Vietnamese phone number format");
            RuleFor(x => x.Relationship).NotEmpty().WithMessage("Next of kin relationship is required");
        }
    }
}
