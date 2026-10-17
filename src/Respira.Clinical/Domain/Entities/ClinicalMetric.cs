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
        /// Version of the metric
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Metric publisher
        /// </summary>
        public string Publisher { get; set; }

        /// <summary>
        /// Metric status, is applied or not
        /// </summary>
        public bool IsApplied { get; set; }

        /// <summary>
        /// Scoring rules
        /// </summary>
        public ICollection<MetricRule> Rules { get; set; } = [];

        public ClinicalMetric(
            string name,
            string code,
            string description,
            string version,
            string publisher,
            bool isApplied,
            ICollection<MetricRule> rules)
        {
            if (rules.Any(r => r.GetType() != typeof(ScoringRule)) && rules.Any(r => r.GetType() != typeof(MajorMinorRule)))
            {
                throw new ArgumentException("All rules belong to a clinical metrics should be of the same type");
            }

            Name = name;
            Code = code;
            Description = description;
            Version = version;
            Publisher = publisher;
            IsApplied = isApplied;
            Rules = rules;
        }

        /// <summary>
        /// This is just a parameterless constructor for EF Core stuff. For normal usage,
        /// it's better to use the parameter constructor
        /// </summary>
        public ClinicalMetric()
        {
            Name = string.Empty;
            Code = string.Empty;
            Description = string.Empty;
            Version = string.Empty;
            Publisher = string.Empty;
        }

        // A metric with no rules belongs to neither system: All() would vacuously be
        // true for both predicates on an empty collection, making every metric both
        // scoring and major/minor at once.
        public bool IsMajorMinorMetric => Rules.Count > 0 && Rules.All(r => r.GetType() == typeof(MajorMinorRule));
        public bool IsScoringMetric => Rules.Count > 0 && Rules.All(r => r.GetType() == typeof(ScoringRule));
    }
}
