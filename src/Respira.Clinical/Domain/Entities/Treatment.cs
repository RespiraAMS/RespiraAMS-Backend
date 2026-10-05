using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Models;

namespace Respira.Clinical.Domain.Entities
{
    public class Treatment : Base
    {
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
        public ICollection<MedicineComposition> Medicines { get; set; } = [];
        public ICollection<Pathogen> Pathogens { get; set; } = [];
        public ICollection<Criterion> Criteria { get; set; } = [];
    }
}
