using FluentValidation;

namespace Respira.Patient.Application.Features.Patients.DischargePatient
{
    public class DischargePatientValidator : AbstractValidator<DischargePatientCommand>
    {
        public DischargePatientValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Patient ID is required");
            RuleFor(x => x.Outcome).IsInEnum().WithMessage("Patient treatment outcome is required");
        }
    }
}
