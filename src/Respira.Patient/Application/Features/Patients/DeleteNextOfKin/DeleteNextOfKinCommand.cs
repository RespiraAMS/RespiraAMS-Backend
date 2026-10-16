using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Patient.Application.Features.Patients.DeleteNextOfKin
{
    public record DeleteNextOfKinCommand : ICommand
    {
        public required Guid Id { get; set; }
    }
}
