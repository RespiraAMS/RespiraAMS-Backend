using Respira.Domain.Enums;
using Respira.ServiceDefaults.Models;

namespace Respira.Domain.Entities
{
    public class SuspectedCause : Base
    {
        public required Guid PathogenId { get; set; }
        public Pathogen Pathogen { get; set; } = null!;
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
    }
}
