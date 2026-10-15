using System.Text.Json.Serialization;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Models;

namespace Respira.Clinical.Domain.Entities
{
    /// <summary>
    /// Metrics rule is a rule used to calculate for a specific criterion in a metric
    /// </summary>
    public abstract class MetricRule : Base
    {
        /// <summary>
        /// Scoring metric ID
        /// </summary>
        public required Guid ClinicalMetricId { get; set; }

        /// <summary>
        /// Scoring metric
        /// </summary>
        [JsonIgnore]
        public ClinicalMetric ClinicalMetric { get; set; } = null!;

        /// <summary>
        /// Criterion ID
        /// </summary>
        public required Guid CriterionId { get; set; }

        /// <summary>
        /// Criterion used to evaluate the score
        /// </summary>
        public Criterion Criterion { get; set; } = null!;
    }

    /// <summary>
    /// Rule that use score system
    /// </summary>
    public class ScoringRule : MetricRule
    {
        /// <summary>
        /// Score function (formula)
        /// </summary>
        public required Formula ScoreFunction { get; set; }

        /// <summary>
        /// The variables used by this scoring rule. This is not database attribute,
        /// but runtime attribute
        /// </summary>
        public IEnumerable<VariableRef> Variables => Criterion.Variables.Concat(ScoreFunction.Variables).DistinctBy(x => x.Code);

        /// <summary>
        /// Get the score
        /// </summary>
        /// <returns>Score</returns>
        public decimal GetScore(IEnumerable<ClinicalObservation> observations)
        {
            observations = observations.ToList();
            // Check if the result is safe to parse
            var satisfied = Criterion.IsCriterionSatisfied(observations);
            {
                // Check if the observations provide enough's information to evaluate the score function
                if (ScoreFunction.Variables.Any(v => !observations.Any(o => o.Variable.Code.Equals(v.Code))))
                {
                    var variablesNeeded = string.Join(",", ScoreFunction.Variables.Select(x => x.Code));
                    throw new InvalidOperationException($"Score function requires observations for variables [{variablesNeeded}]");
                }

                var score = ScoreFunction.ToExpression(observations).Evaluate();
                if (score is decimal y)
                {
                    return satisfied ? y : 0;
                }

                throw new Exception($"Score function result type should be numeric, but get {score.GetType()}");
            }
        }
    }

    /// <summary>
    /// This rule represent a major/minor system (like IDSA/ATS), where it didn't use
    /// score, but instead count the number of major/minor criteria matched
    /// </summary>
    public class MajorMinorRule : MetricRule
    {
        public required bool IsMajor { get; set; }

        /// <summary>
        /// The variables used by this major/minor rule. This is not database attribute,
        /// but runtime attribute
        /// </summary>
        public IEnumerable<VariableRef> Variables => Criterion.Variables.DistinctBy(x => x.Code);
    }
}
