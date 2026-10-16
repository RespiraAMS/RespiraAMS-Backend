using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Patient.Application.Features.Patients.UpdateNextOfKin
{
    public record UpdateNextOfKinCommand : ICommand
    {
        public required Guid Id { get; set; }
        public required string FullName { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Relationship { get; set; }
    }
}
