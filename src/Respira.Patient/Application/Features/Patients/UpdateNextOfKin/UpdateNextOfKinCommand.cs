using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Patient.Application.Features.Patients.UpdateNextOfKin
{
    public record UpdateNextOfKinCommand : ICommand
    {
        /// <summary>
        /// Next of kin ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Next of kin full name
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
}
