using System.Text.Json.Serialization;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Models;

namespace Respira.Clinical.Domain.Entities
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

        public IEnumerable<VariableRef> Variables => Criterion.Variables.DistinctBy(x => x.Code);

        public bool IsFactorSastified(IEnumerable<ClinicalObservation> observations)
        {
            var result = Criterion.IsCriterionSastisfied(observations);
            if (result is bool x)
            {
                return x;
            }

            throw new Exception($"Criterion result type should be boolean, but get {result.GetType()}");
        }
    }
}
