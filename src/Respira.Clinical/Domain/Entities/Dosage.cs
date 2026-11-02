using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Models;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Domain.Entities
{
    public class Dosage : Base
    {
        /// <summary>
        /// Antibiotic ID
        /// </summary>
        public required Guid AntibioticId { get; set; }

        /// <summary>
        /// Antibiotic
        /// </summary>
        public Antibiotic Antibiotic { get; set; } = null!;

        /// <summary>
        /// Route of administration
        /// </summary>
        public required RouteOfAdministration RouteOfAdministration { get; set; }

        /// <summary>
        /// Antibiotic dose. This string will not have any special format,
        /// since the value for dose is very flexible, and it's mostly read from
        /// </summary>
        public required string Dose { get; set; }

        /// <summary>
        /// CrCl range. When null, then that means this is a standard dose
        /// (no need for adjusting dose). If not null, then the this value
        /// indicate the range of CrCl that should be applied when adjusting dosage
        /// for patient
        /// </summary>
        public Range? Crcl { get; set; }

        /// <summary>
        /// Whether this dosage is a standard dose or not
        /// </summary>
        public bool IsStandardDose => Crcl is null;
    }
}
