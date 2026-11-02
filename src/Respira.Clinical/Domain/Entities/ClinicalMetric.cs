using Respira.ServiceDefaults.Models;

namespace Respira.Clinical.Domain.Entities
{
    /// <summary>
    /// This is the class represent a scoring metric, like CURB-65 or PSI
    /// </summary>
    public class ClinicalMetric : FullTextSearch
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
        /// Version of the metric
        /// </summary>
        public required string Version { get; set; }

        /// <summary>
        /// Metric publisher
        /// </summary>
        public required string Publisher { get; set; }

        /// <summary>
        /// Metric status, is applied or not
        /// </summary>
        public required bool IsApplied { get; set; }

        /// <summary>
        /// Scoring rules
        /// </summary>
        public ICollection<MetricRule> Rules
        {
            get;
            set
            {
                if (value.Any(r => r.GetType() != typeof(ScoringRule)) && value.Any(r => r.GetType() != typeof(MajorMinorRule)))
                {
                    throw new ArgumentException("All rules belong to a clinical metrics should be of the same type");
                }
                field = value;
            }
        } = [];

        // A metric with no rules belongs to neither system: All() would vacuously be
        // true for both predicates on an empty collection, making every metric both
        // scoring and major/minor at once.
        public bool IsMajorMinorMetric => Rules.Count > 0 && Rules.All(r => r.GetType() == typeof(MajorMinorRule));
        public bool IsScoringMetric => Rules.Count > 0 && Rules.All(r => r.GetType() == typeof(ScoringRule));
    }
}
