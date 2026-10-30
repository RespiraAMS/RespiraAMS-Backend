using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Models;

namespace Respira.Clinical.Domain.Entities
{
    public class Treatment : Base
    {
        /// <summary>
        /// Severity this protocol should be applied
        /// </summary>
        public required Severity Severity { get; set; }

        /// <summary>
        /// Treatment site this protocol should be applied
        /// </summary>
        public required TreatmentSite TreatmentSite { get; set; }

        /// <summary>
        /// List of treatment
        /// </summary>
        public ICollection<MedicineComposition> Medicines { get; set; } = [];

        /// <summary>
        /// List of pathogens this protocol should be applied. This list
        /// is an AND operator, which means that the protocol should only
        /// applied when patient is diagnosis to likely be affected of
        /// ALL pathogens this list has
        /// </summary>
        public ICollection<Pathogen> Pathogens { get; set; } = [];

        /// <summary>
        /// List of criteria this protocol should be applied. This list
        /// is an AND operator, which means that the protocol should only
        /// applied when patient is diagnosis to likely have ALL criteria
        /// this list has
        /// </summary>
        public ICollection<Criterion> Criteria { get; set; } = [];
    }
}
