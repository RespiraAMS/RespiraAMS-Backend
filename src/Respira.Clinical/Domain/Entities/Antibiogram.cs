using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Models;

namespace Respira.Clinical.Domain.Entities
{
    public class Antibiogram : Base
    {
        public required MicCategory MicCategory { get; set; }
        public required Guid PathogenId { get; set; }
        public Pathogen Pathogen { get; set; } = null!;
        public ICollection<Antibiotic> Antibiotics { get; set; } = [];
    }
}
