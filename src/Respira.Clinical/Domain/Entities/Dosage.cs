using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Models;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Domain.Entities
{
    public class Dosage : Base
    {
        public required Guid AntibioticId { get; set; }
        public Antibiotic Antibiotic { get; set; } = null!;
        public required RouteOfAdministration RouteOfAdministration { get; set; }
        public required string Dose { get; set; }
        public Range? Crcl { get; set; }
    }
}
