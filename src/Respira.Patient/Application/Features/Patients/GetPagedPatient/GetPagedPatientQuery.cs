using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Patient.Application.Features.Patients.GetPagedPatient
{

    public record PatientFilter
    {
        /// <summary>
        /// Patient's gender
        /// </summary>
        public Gender? Gender { get; set; }

        /// <summary>
        /// Minimum age of patient
        /// </summary>
        public int? MinAge { get; set; }

        /// <summary>
        /// Maximum age of patient
        /// </summary>
        public int? MaxAge { get; set; }
    }

    public record GetPagedPatientQuery : IQuery
    {
        /// <summary>
        /// Pagination param
        /// </summary>
        public required PaginationParam Param { get; set; }

        /// <summary>
        /// Patient filter
        /// </summary>
        public PatientFilter? Filter { get; set; } = new();
    }

    public record PagedPatientItem
    {
        /// <summary>
        /// Patient ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Patient's full name
        /// </summary>
        public required string FullName { get; set; }

        /// <summary>
        /// Patient's date of birth
        /// </summary>
        public required DateOnly DateOfBirth { get; set; }

        /// <summary>
        /// Patient's age
        /// </summary>
        public required int Age { get; set; }

        /// <summary>
        /// Patient's gender
        /// </summary>
        public required Gender Gender { get; set; }

        /// <summary>
        /// Patient's medical record code
        /// </summary>
        public required string MedicalRecordCode { get; set; }

        /// <summary>
        /// Patient's treatment outcome
        /// </summary>
        public required TreatmentOutcome TreatmentOutcome { get; set; }
    }
}
