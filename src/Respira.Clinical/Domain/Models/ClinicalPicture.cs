using Respira.Clinical.Domain.Entities;

namespace Respira.Clinical.Domain.Models
{
    public record ClinicalPicture
    {
        public required IEnumerable<ClinicalObservation> Observations { get; set; }
        public required IEnumerable<Antibiotic> Allergies { get; set; }
    }
}