using System.Text.Json.Serialization;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Models;

namespace Respira.Clinical.Domain.Entities
{
    /// <summary>
    /// Metrics rule is a rule used to calculate for a specific criterion in a metric
    /// </summary>
    public abstract class MetricsRule : Base
    {
        /// <summary>
        /// Scoring metric ID
        /// </summary>
        public required Guid ClinicalMetricsId { get; set; }

        /// <summary>
        /// Scoring metric
        /// </summary>
        [JsonIgnore]
        public ClinicalMetrics ClinicalMetrics { get; set; } = null!;

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
    public class ScoringRule : MetricsRule
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
            // // A rule cannot be evaluated when any of its variables has no observation.
            // // Skip the rule (contribute 0) so the remaining criteria are still counted.
            // if (Variables.Any(v => !observations.Any(o => o.Variable.Code.Equals(v.Code))))
            // {
            //     return 0;
            // }

            // Check if the result is safe to parse
            var sastified = Criterion.IsCriterionSatisfied(observations);
            if (sastified is bool x)
            {
                // Check if the obsevations provide enoughs information to evaluate the score function
                if (ScoreFunction.Variables.Any(v => !observations.Any(o => o.Variable.Code.Equals(v.Code))))
                {
                    var variablesNeeded = string.Join(",", ScoreFunction.Variables.Select(x => x.Code));
                    throw new InvalidOperationException($"Score function requires observations for variables [{variablesNeeded}]");
                }

                var score = ScoreFunction.ToExpression(observations).Evaluate();
                if (score is decimal y)
                {
                    return x ? y : 0;
                }

                throw new Exception($"Score function result type should be numeric, but get {score.GetType()}");
            }

            throw new Exception($"Criterion result type should be boolean, but get {sastified.GetType()}");
        }
    }

    /// <summary>
    /// This rule represent a major/minor system (like IDSA/ATS), where it didn't use
    /// score, but instead count the number of major/minor criteria matched
    /// </summary>
    public class MajorMinorRule : MetricsRule
    {
        public required bool IsMajor { get; set; }
    }
}
