using Respira.ServiceDefaults.Models;

namespace Respira.Domain.Entities
{
    public class Pathogen : Base
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public IEnumerable<RiskFactor> RiskFactors { get; set; } = [];
    }
}
