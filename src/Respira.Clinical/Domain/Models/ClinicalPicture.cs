using Respira.Clinical.Domain.Enums;

namespace Respira.Clinical.Domain.Models
{
    public record ClinicalPicture
    {
        /// <summary>
        /// Clinical observations
        /// </summary>
        public required IEnumerable<ClinicalObservation> Observations { get; set; }

        /// <summary>
        /// Patient allergy status
        /// </summary>
        public required Allergy Allergy { get; set; }

        /// <summary>
        /// Special medical history that the system does not cover.
        /// This would be used to warn doctor before making final decision,
        /// and audit when revisit treatment
        /// </summary>
        public IEnumerable<string> SpecialMedicalHistory { get; set; } = [];

        /// <summary>
        /// Other active infections, excluding pneumonia.
        /// </summary>
        public IEnumerable<InfectionType> OtherInfections { get; set; } = [];

        /// <summary>
        /// Microbial samples
        /// </summary>
        public IEnumerable<MicrobialSample> MicrobialSamples { get; set; } = [];
    }
}
