using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Patient.Application.Features.Patients.DischargePatient
{
    public record DischargePatientCommand : ICommand
    {
        public required Guid Id { get; set; }
        public required TreatmentOutcome Outcome { get; set; }
    }
}
