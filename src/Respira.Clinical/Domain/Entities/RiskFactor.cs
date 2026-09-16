using System.Text.Json.Serialization;
using Respira.Domain.Models;
using Respira.ServiceDefaults.Models;

namespace Respira.Domain.Entities
{
    /// <summary>
    /// Risk factors are used to evaluate if patient is at risk of infection with a specefic pathogen.
    /// A risk factor is more credible than <see cref="SuspectedCause"/> if it is evaluated as true.
    /// </summary>
    public class RiskFactor : Base
    {
        /// <summary>
        /// Pathogen ID
        /// </summary>
        public required Guid PathogenId { get; set; }

        /// <summary>
        /// Pathogen
        /// </summary>
        [JsonIgnore]
        public Pathogen Pathogen { get; set; } = null!;

        /// <summary>
        /// Criterion ID
        /// </summary>
        public required Guid CriterionId { get; set; }

        /// <summary>
        /// Criterion
        /// </summary>
        public Criterion Criterion { get; set; } = null!;

        /// <summary>
        /// A risk factor may have priority, but since they don't have any official scoring
        /// rule, we don't reuse the <see cref="ScoringRule"/> entity, but instead use this
        /// attribute to indicate the priority of the risk factor.
        /// The smaller the value, the higher the priority. Highest priority is 1.
        /// </summary>
        public required int Priority { get; set; }

        public IEnumerable<ClinicalVariable> Variables => Criterion.Variables.DistinctBy(x => x.Code);

        public bool IsFactorSasified(IEnumerable<ClinicalObservation> observations)
        {
            var result = Criterion.IsCriterionSatisfied(observations);
            if (result is bool x)
            {
                return x;
            }

            throw new Exception($"Criterion result type should be boolean, but get {result.GetType()}");
        }
    }
}
