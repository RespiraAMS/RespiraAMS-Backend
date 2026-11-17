using Respira.ServiceDefaults.Models;

namespace Respira.Clinical.Domain.Entities
{
    public class Treatment : Base
    {
        /// <summary>
        /// TreatmentProtocol protocol ID
        /// </summary>
        public required Guid TreatmentProtocolId { get; set; }

        /// <summary>
        /// TreatmentProtocol protocol
        /// </summary>
        public TreatmentProtocol TreatmentProtocol { get; set; } = null!;

        /// <summary>
        /// Antibiotic combination
        /// </summary>
        public ICollection<Antibiotic> Combination { get; set; } = [];
    }
}
