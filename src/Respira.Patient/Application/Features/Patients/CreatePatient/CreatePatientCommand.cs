using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Patient.Application.Features.Patients.CreatePatient
{
    public record CreatePatientCommand : ICommand
    {
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

    public record CreatePatientResult(Guid Id);
}
