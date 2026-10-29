using Respira.Clinical.Domain.Enums;

namespace Respira.Clinical.Domain.Models
{
    public record MicrobialSample
    {
        /// <summary>
        /// Microbial sample type
        /// </summary>
        public required MicrobialSampleType SampleType { get; set; }

        /// <summary>
        /// MIC category
        /// </summary>
        public required MicCategory MicCategory { get; set; }

        /// <summary>
        /// Pathogen ID
        /// </summary>
        public required Guid PathogenId { get; set; }
    }
}
