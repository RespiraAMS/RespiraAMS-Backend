using Respira.ServiceDefaults.Models;

namespace Respira.Clinical.Domain.Entities
{
    /// <summary>
    /// This is the class represent a scoring metric, like CURB-65 or PSI
    /// </summary>
    public class ClinicalMetrics : Base
    {
        /// <summary>
        /// Scoring metric name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Scoring metric code. Preferably follow LOINC code
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Scoring metric description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Scoring rules
        /// </summary>
        public ICollection<MetricsRule> Rules { get; set; } = [];

        public ClinicalMetrics(string name, string code, string description, ICollection<MetricsRule> rules)
        {
            if (!rules.All(r => r.GetType() == typeof(ScoringRule)) && !rules.All(r => r.GetType() == typeof(MajorMinorRule)))
            {
                throw new ArgumentException("All rules belong to a clinical metrics should be of the same type");
            }

            Name = name;
            Code = code;
            Description = description;
            Rules = rules;
        }

        /// <summary>
        /// This is just a parameterless constructor for EF Core stuff. For normal usage,
        /// it's better to use the parameter constructor
        /// </summary>
        public ClinicalMetrics()
        {
            Name = string.Empty;
            Code = string.Empty;
            Description = string.Empty;
        }

        // A metric with no rules belongs to neither system: All() would vacuously be
        // true for both predicates on an empty collection, making every metric both
        // scoring and major/minor at once.
        public bool IsMajorMinorMetric => Rules.Count > 0 && Rules.All(r => r.GetType() == typeof(MajorMinorRule));
        public bool IsScoringMetric => Rules.Count > 0 && Rules.All(r => r.GetType() == typeof(ScoringRule));
    }
}
