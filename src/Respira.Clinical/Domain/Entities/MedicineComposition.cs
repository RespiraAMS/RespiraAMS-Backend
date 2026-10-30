using Respira.ServiceDefaults.Models;

namespace Respira.Clinical.Domain.Entities
{
    public class MedicineComposition : Base
    {
        /// <summary>
        /// Treatment protocol ID
        /// </summary>
        public required Guid TreatmentId { get; set; }

        /// <summary>
        /// Treatment protocol
        /// </summary>
        public Treatment Treatment { get; set; } = null!;

        /// <summary>
        /// Antibiotic combination
        /// </summary>
        public ICollection<Antibiotic> Antibiotics { get; set; } = [];
    }
}
