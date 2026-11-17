using Respira.Patient.Domain.Utils;
using Respira.ServiceDefaults.Models;

namespace Respira.Patient.Domain.Entities
{
    public class NextOfKin : Base
    {
        /// <summary>
        /// Patient ID
        /// </summary>
        public required Guid PatientId { get; set; }

        /// <summary>
        /// Patient associated with this next of kin
        /// </summary>
        public Patient Patient { get; set; } = null!;

        /// <summary>
        /// Kin's full name
        /// </summary>
        public required string FullName { get; set => field = NameNormalizer.NameNormalize(value); }

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
