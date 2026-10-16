using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Patient.Application.Features.Patients.GetPatientById
{
    public record GetPatientByIdQuery : IQuery
    {
        public required Guid Id { get; set; }
    }

    public record NextOfKinItem(Guid Id, string FullName, string PhoneNumber, string Relationship);
    public record TreatmentItem(Guid Id);

    public record PatientResult
    {
        public required Guid Id { get; set; }
        public required string FullName { get; set; }
        public required string PhoneNumber { get; set; }
        public required DateOnly DateOfBirth { get; set; }
        public required Gender Gender { get; set; }
        public required decimal Height { get; set; }
        public required decimal Weight { get; set; }
        public required string MedicalRecordCode { get; set; }
        public required string HealthInsuranceCard { get; set; }
        public required string Address { get; set; }
        public required string City { get; set; }
        public required DateTimeOffset Admission { get; set; }
        public DateTimeOffset? Discharge { get; set; }
        public required TreatmentOutcome TreatmentOutcome { get; set; }
        public IEnumerable<NextOfKinItem> NextOfKins { get; set; } = [];
        public IEnumerable<TreatmentItem> Treatments { get; set; } = [];
    }
}
