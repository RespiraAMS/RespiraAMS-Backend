using Domain.Enums;

namespace Application.Features.Patients.DischargePatient;

public class DischargePatientValidator : AbstractValidator<DischargePatientCommand>
{
    public DischargePatientValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Patient ID is required");
        RuleFor(x => x.Status)
            .IsInEnum()
            // Since this is discharge, so it can only either be recovered or death, not in treatment 
            .Must(status => status is PatientStatus.Death or PatientStatus.Recovered)
            .WithMessage("Status is required");
    }
}