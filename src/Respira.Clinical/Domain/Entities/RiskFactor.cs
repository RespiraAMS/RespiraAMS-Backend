using Respira.ServiceDefaults.Models;

namespace Respira.Domain.Entities
{
    public class RiskFactor : Base
    {
        public required Guid PathogenId { get; set; }
        public Pathogen Pathogen { get; set; } = null!;
        public required Criterion Criterion { get; set; }
    }
}
