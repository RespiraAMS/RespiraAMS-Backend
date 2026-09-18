using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Models;

namespace Respira.Clinical.Domain.Entities
{
    public class Treatment : Base
    {
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
        public List<Guid> MedicineIds { get; set; } = [];
        public List<Antibiotic> Medicines { get; set; } = [];
        public List<Guid> PathogenIds { get; set; } = [];
        public List<Pathogen> Pathogens { get; set; } = [];
        public List<Guid> CriteriaIds { get; set; } = [];
        public List<Criterion> Criteria { get; set; } = [];
    }
}
