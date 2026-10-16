using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Patient.Application.Features.Patients.GetPatientById
{
    public record GetPatientByIdQuery : IQuery
    {
        /// <summary>
        /// Patient ID
        /// </summary>
        public required Guid Id { get; set; }
    }

    /// <summary>
    /// Patient next of kin result
    /// </summary>
    /// <param name="Id">Next of kin ID</param>
    /// <param name="FullName">Next of kin full name</param>
    /// <param name="PhoneNumber">Next of kin phone number</param>
    /// <param name="Relationship">Next of kin relationship with patient</param>
    public record NextOfKinItem(Guid Id, string FullName, string PhoneNumber, string Relationship);

    /// <summary>
    /// Patient treatment result
    /// </summary>
    /// <param name="Id">Treatment ID</param>
    public record TreatmentItem(Guid Id);

    public record PatientResult
    {
        /// <summary>
        /// Patient ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Patient fullname
        /// </summary>
        public required string FullName { get; set; }

        /// <summary>
        /// Patient phone number
        /// </summary>
        public required string PhoneNumber { get; set; }

        /// <summary>
        /// Patient date of birth
        /// </summary>
        public required DateOnly DateOfBirth { get; set; }

        /// <summary>
        /// Patient gender
        /// </summary>
        public required Gender Gender { get; set; }

        /// <summary>
        /// Patient height
        /// </summary>
        public required decimal Height { get; set; }

        /// <summary>
        /// Patient weight
        /// </summary>
        public required decimal Weight { get; set; }

        /// <summary>
        /// Patient medical record code
        /// </summary>
        public required string MedicalRecordCode { get; set; }

        /// <summary>
        /// Patient health insurance card number
        /// </summary>
        public required string HealthInsuranceCard { get; set; }

        /// <summary>
        /// Patient address
        /// </summary>
        public required string Address { get; set; }

        /// <summary>
        /// Patient city
        /// </summary>
        public required string City { get; set; }

        /// <summary>
        /// Patient admission datetime
        /// </summary>
        public required DateTimeOffset Admission { get; set; }

        /// <summary>
        /// Patient discharge datetime
        /// </summary>
        public DateTimeOffset? Discharge { get; set; }

        /// <summary>
        /// Patient treatment outcome
        /// </summary>
        public required TreatmentOutcome TreatmentOutcome { get; set; }

        /// <summary>
        /// Patient next of kin
        /// </summary>
        public IEnumerable<NextOfKinItem> NextOfKins { get; set; } = [];

        /// <summary>
        /// Patient treatments
        /// </summary>
        public IEnumerable<TreatmentItem> Treatments { get; set; } = [];
    }
}
