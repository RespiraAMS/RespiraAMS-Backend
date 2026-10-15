using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Patient.Application.Features.Patients.UpdatePatient
{
    public record UpdatePatientCommand : ICommand
    {
        public required Guid Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string PhoneNumber { get; set; }
        public required DateOnly DateOfBirth { get; set; }
        public required Gender Gender { get; set; }
        public required decimal Height { get; set; }
        public required decimal Weight { get; set; }
        public required string MedicalRecordCode { get; set; }
        public required string HealthInsuranceCard { get; set; }
        public required string Address { get; set; }
        public required string City { get; set; }
    }
}
