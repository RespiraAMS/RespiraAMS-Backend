using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Patient.Application.Features.Patients.AddNextOfKin
{
    public record AddNextOfKinCommand : ICommand
    {
        /// <summary>
        /// Patient ID
        /// </summary>
        public required Guid PatientId { get; set; }

        /// <summary>
        /// Kin's full name
        /// </summary>
        public required string FullName { get; set; }

        /// <summary>
        /// Kin's phone number. This should be unique per patient,
        /// and should follow Vietnamese phone number format
        /// </summary>
        public required string PhoneNumber { get; set; }

        /// <summary>
        /// Kin's relationship with patient (parent, sibling,...)
        /// </summary>
        public required string Relationship { get; set; }
    }

    /// <summary>
    /// Add next of kin result
    /// </summary>
    /// <param name="Id">Created next of kin ID</param>
    public record AddNextOfKinResult(Guid Id);
}
