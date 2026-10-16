using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Patient.Application.Features.Patients.GetPagedPatient
{

    public record PatientFilter
    {
        public string? Name { get; set; }
        public string? MedicalRecordCode { get; set; }
        public Gender? Gender { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
    }

    public record GetPagedPatientQuery : IQuery
    {
        public required PaginationParam Param { get; set; }
        public PatientFilter? Filter { get; set; } = new();
    }

    public record PagedPatientItem
    {
        public required Guid Id { get; set; }
        public required string FullName { get; set; }
        public required DateOnly DateOfBirth { get; set; }
        public required int Age { get; set; }
        public required Gender Gender { get; set; }
        public required string MedicalRecordCode { get; set; }
        public required TreatmentOutcome TreatmentOutcome { get; set; }
    }
}
