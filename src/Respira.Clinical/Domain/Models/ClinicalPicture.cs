using Respira.Clinical.Domain.Entities;

namespace Respira.Clinical.Domain.Models
{
    public record ClinicalPicture
    {
        public required List<ClinicalObservation> Observations { get; set; }
        public required List<Antibiotic> Allergies { get; set; }
    }
}
