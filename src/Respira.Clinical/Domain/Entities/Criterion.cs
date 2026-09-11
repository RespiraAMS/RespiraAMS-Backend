using Respira.Domain.Enums;
using Respira.Domain.Models;
using Respira.ServiceDefaults.Models;

namespace Respira.Domain.Entities
{
    /// <summary>
    /// Criterion used to evaluate condition, severity,...
    /// </summary>
    public class Criterion : Base
    {
        /// <summary>
        /// Criterion name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A criterion should be evaluated into a boolean value
        /// </summary>
        public Formula Formula { get; set; }

        /// <summary>
        /// The variables used by this criterion
        /// </summary>
        public IEnumerable<ClinicalVariable> Variables => Formula.Variables;

        public Criterion(string name, Formula formula)
        {
            if (formula.ResultType != ExpressionResultType.Boolean)
            {
                throw new ArgumentException("Formula result type should be boolean");
            }

            Name = name;
            Formula = formula;
        }

        /// <summary>
        /// Evaludate if the criterion is sastisfied by the given observations
        /// </summary>
        /// <param name="observations">Clinical observation</param>
        /// <returns>True if criterion is satisfied, false otherwise</returns>
        public bool IsCriterionSatisfied(IEnumerable<ClinicalObservation> observations)
        {
            var result = Formula.ToExpression(observations).Evaluate();
            if (result is bool x)
            {
                return x;
            }

            throw new Exception($"Formula result type should be boolean, but get {result.GetType()}");
        }
    }
}
