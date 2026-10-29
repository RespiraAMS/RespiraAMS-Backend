using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;

namespace Respira.Clinical.Domain.Models
{
    public class Allergy
    {
        /// <summary>
        /// Type of allergy
        /// </summary>
        public required AllergyType Type { get; set; }

        /// <summary>
        /// The list of antibiotic that patient's allergic to
        /// </summary>
        public IEnumerable<Antibiotic> AllergicAntibiotics { get; set; } = [];

        /// <summary>
        /// Other allergy, corresponding to <see cref="AllergyType.Unknown"/> or
        /// <see cref="AllergyType.NonRegisteredAntibiotic"/>
        /// </summary>
        public string? OtherAllergy { get; set; }

        /// <summary>
        /// Check if this allergy object is valid
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValidAllergy()
        {
            return Type switch
            {
                AllergyType.No => !AllergicAntibiotics.Any() && OtherAllergy is null,
                AllergyType.Yes => AllergicAntibiotics.Any() && OtherAllergy is null,
                _ => !AllergicAntibiotics.Any() && OtherAllergy is not null
            };
        }
    }
}
