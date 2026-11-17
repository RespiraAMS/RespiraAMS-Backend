using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Models;

namespace Respira.Clinical.Domain.Entities
{
    public class Antibiogram : Base
    {
        /// <summary>
        /// MIC category
        /// </summary>
        public required MicCategory MicCategory { get; set; }

        /// <summary>
        /// Pathogen ID
        /// </summary>
        public required Guid PathogenId { get; set; }

        /// <summary>
        /// Pathogen
        /// </summary>
        public Pathogen Pathogen { get; set; } = null!;

        /// <summary>
        /// List of antibiotics used to treat this pathogen.
        /// Note that, this list is a OR semantic (which mean that
        /// there is no combination, only 1 antibiotic is used), that's
        /// why the "has no common ancestor" rule won't be applied to this
        /// list, unlike <see cref="TreatmentProtocol" />
        /// </summary>
        public ICollection<Antibiotic> Antibiotics { get; set; } = [];
    }
}
