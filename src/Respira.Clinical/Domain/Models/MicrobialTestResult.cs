using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;

namespace Respira.Clinical.Domain.Models
{
    public class MicrobialTestResult
    {
        public required MicrobialSampleType MicrobialSampleType { get; set; }
        public required Pathogen Pathogen { get; set; }
        public required MicCategory MicCategory { get; set; }
        public required IEnumerable<Antibiotic> Antibiotics { get; set; }
    }
}
