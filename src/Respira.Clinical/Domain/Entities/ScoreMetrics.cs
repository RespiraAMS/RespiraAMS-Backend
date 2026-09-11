using Respira.ServiceDefaults.Models;

namespace Respira.Domain.Entities
{
    /// <summary>
    /// This is the class represent a scoring metric, like CURB-65 or PSI
    /// </summary>
    public class ScoreMetrics : Base
    {
        /// <summary>
        /// Scoring metric name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Scoring metric code. Preferably follow LOINC code
        /// </summary>
        public required string Code { get; set; }

        /// <summary>
        /// Scoring metric description
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Scoring rules
        /// </summary>
        public IEnumerable<ScoringRule> ScoringRules { get; set; } = [];
    }
}
