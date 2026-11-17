using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Patient.Application.Features.Patients.SearchPatient
{
    public record SearchPatientQuery : IQuery
    {
        public required PaginationParam Param { get; set; }
        public required string SearchTerm { get; init; }
    }

    public record SearchPatientItem
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

        /// <summary>
        /// Search rank (most relevant to search term)
        /// </summary>
        public float Rank { get; set; }
    }
}
