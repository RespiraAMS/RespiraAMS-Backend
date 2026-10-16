using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Patient.Application.Features.Patients.DeletePatient
{
    public record DeletePatientCommand : ICommand
    {
        public required Guid Id { get; set; }
    }
}
