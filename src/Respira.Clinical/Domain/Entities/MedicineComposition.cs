using Respira.ServiceDefaults.Models;

namespace Respira.Clinical.Domain.Entities
{
    public class MedicineComposition : Base
    {
        public required Guid TreatmentId { get; set; }
        public Treatment Treatment { get; set; } = null!;
        public ICollection<Antibiotic> Antibiotics { get; set; } = [];
    }
}
