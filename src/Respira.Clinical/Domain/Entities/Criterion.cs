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

        /// <summary>
        /// This is just a parameterless constructor for EF Core
        /// </summary>
        private Criterion()
        {
            Name = string.Empty;
            Formula = new BooleanConstantFormula(true);
        }

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
            // Check if all the variables needed by this criterion are present.
            // If any missing, this criterion will result to false
            // NOTE: even though OR operation with 1 operand can be evaluated to true,
            // or AND operation with 1 operand can be evaluated to false, 
            // can theoratically works, we will NOT support this case to keep the logic simple
            // and consistent with the other expressions

            if (Variables.Any(v => !observations.Any(o => o.Variable.Code.Equals(v.Code))))
            {
                return false;
            }

            var result = Formula.ToExpression(observations).Evaluate();
            if (result is bool x)
            {
                return x;
            }

            throw new Exception($"Formula result type should be boolean, but get {result.GetType()}");
        }
    }
}
