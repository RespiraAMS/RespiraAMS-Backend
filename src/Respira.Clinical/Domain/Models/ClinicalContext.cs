using Respira.Domain.Entities;

namespace Respira.Domain.Models
{
    /// <summary>
    /// Clinical context. This class contains all information required to perform
    /// clinical diagnosis
    /// </summary>
    public class ClinicalContext
    {
        public required IEnumerable<ClinicalVariable> Variables { get; set; }
        public required IEnumerable<ScoreMetrics> Metrics { get; set; }
        public required IEnumerable<Pathogen> Pathogens { get; set; }
        public required IEnumerable<SuspectedCause> SuspectedCauses { get; set; }
    }
}
