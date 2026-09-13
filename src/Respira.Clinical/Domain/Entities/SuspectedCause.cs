using Respira.Domain.Enums;
using Respira.ServiceDefaults.Models;

namespace Respira.Domain.Entities
{
    /// <summary>
    /// This class represent suspected causes of a patient knowing their severity and treatment site
    /// after a clinical assessment
    /// </summary>
    public class SuspectedCause : Base
    {
        /// <summary>
        /// Pathogen ID
        /// </summary>
        public required Guid PathogenId { get; set; }

        /// <summary>
        /// Pathogen
        /// </summary>
        public Pathogen Pathogen { get; set; } = null!;

        /// <summary>
        /// Severity
        /// </summary>
        public required Severity Severity { get; set; }

        /// <summary>
        /// Treatment site
        /// </summary>
        public required TreatmentSite TreatmentSite { get; set; }
    }
}
