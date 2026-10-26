using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Models;

namespace Respira.Clinical.Domain.Entities
{
    /// <summary>
    /// Criterion used to evaluate condition, severity,...
    /// </summary>
    public class Criterion : Base
    {
        /// <summary>
        /// Criterion name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// A criterion should be evaluated into a boolean value
        /// </summary>
        public required Formula Formula
        {
            get;
            set
            {
                if (value.ResultType != ExpressionResultType.Boolean)
                {
                    throw new ArgumentException("Formula result type should be boolean");
                }

                field = value;
            }
        }

        /// <summary>
        /// The variables used by this criterion
        /// </summary>
        public IEnumerable<VariableRef> Variables => Formula.Variables;

        /// <summary>
        /// Evaluate if the criterion is satisfied by the given observations
        /// </summary>
        /// <param name="observations">Clinical observation</param>
        /// <returns>True if criterion is satisfied, false otherwise</returns>
        public bool IsCriterionSatisfied(IEnumerable<ClinicalObservation> observations)
        {
            try
            {
                var result = Formula.ToExpression(observations).Evaluate();
                if (result is bool x)
                {
                    return x;
                }

                // There shouldn't be any way for this to reach, unless the setter is
                // somehow modified/broken
                throw new Exception($"Formula result type should be boolean, but get {result.GetType()}");
            }
            catch (ArgumentException)
            {
                // If the formula failed to construct/evaluate, then this criterion is not satisfied
                return false;
            }
        }
    }
}
