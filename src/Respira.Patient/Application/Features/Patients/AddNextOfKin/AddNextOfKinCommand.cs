using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Patient.Application.Features.Patients.AddNextOfKin
{
    public record AddNextOfKinCommand : ICommand
    {
        public required Guid PatientId { get; set; }
        public required string FullName { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Relationship { get; set; }
    }

    public record AddNextOfKinResult(Guid Id);
}
