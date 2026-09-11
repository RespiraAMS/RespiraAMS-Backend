using Respira.Domain.Models;
using Respira.ServiceDefaults.Models;

namespace Respira.Domain.Entities
{
    /// <summary>
    /// Scoring rule is used to calculate a score for a specific criterion in a scoring metric
    /// </summary>
    public class ScoringRule : Base
    {
        /// <summary>
        /// Scoring metric ID
        /// </summary>
        public required Guid ScoreMetricsId { get; set; }

        /// <summary>
        /// Scoring metric
        /// </summary>
        public ScoreMetrics ScoreMetrics { get; set; } = null!;

        /// <summary>
        /// Criterion ID
        /// </summary>
        public required Guid CriterionId { get; set; }

        /// <summary>
        /// Criterion used to evaluate the score
        /// </summary>
        public Criterion Criterion { get; set; } = null!;

        /// <summary>
        /// Score function (formula)
        /// </summary>
        public required Formula ScoreFunction { get; set; }

        /// <summary>
        /// The variables used by this scoring rule. This is not database attribute,
        /// but runtime attribute
        /// </summary>
        public IEnumerable<ClinicalVariable> Variables => Criterion.Variables.Concat(ScoreFunction.Variables).DistinctBy(x => x.Code);

        /// <summary>
        /// Get the score
        /// </summary>
        /// <returns>Score</returns>
        public decimal GetScore(IEnumerable<ClinicalObservation> observations)
        {
            // A rule cannot be evaluated when any of its variables has no observation.
            // Skip the rule (contribute 0) so the remaining criteria are still counted.
            if (Variables.Any(v => !observations.Any(o => o.Variable.Code.Equals(v.Code))))
            {
                return 0;
            }

            // Check if the result is safe to parse
            var sastified = Criterion.IsCriterionSatisfied(observations);
            if (sastified is bool x)
            {
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
}
