using Application.Features.Doctors;
using FluentValidation;

namespace Application.Features.Doctors.Create.Commands;

public class CreateDoctorValidator : AbstractValidator<CreateDoctorCommand>
{
    public CreateDoctorValidator()
    {
        RuleFor(x => x.CitizenIdentificationNumber)
            .NotEmpty()
            .WithMessage("Citizen Identification Number is required")
            .Matches(DoctorValidationRules.CitizenIdentificationNumberPattern)
            .WithMessage("Citizen Identification Number must be a valid Vietnam CCCD/CMND (9 or 12 digits)");
    }
}
