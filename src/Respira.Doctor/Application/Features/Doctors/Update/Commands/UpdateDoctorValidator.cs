using Application.Features.Doctors;
using FluentValidation;

namespace Application.Features.Doctors.Update.Commands;

public class UpdateDoctorValidator : AbstractValidator<UpdateDoctorCommand>
{
    public UpdateDoctorValidator()
    {
        RuleFor(x => x.CitizenIdentificationNumber)
            .NotEmpty()
            .WithMessage("Citizen Identification Number is required")
            .Matches(DoctorValidationRules.CitizenIdentificationNumberPattern)
            .WithMessage("Citizen Identification Number must be a valid Vietnam CCCD/CMND (9 or 12 digits)");
    }
}
