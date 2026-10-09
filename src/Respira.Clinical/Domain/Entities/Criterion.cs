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
        public string Name { get; set; }

        /// <summary>
        /// A criterion should be evaluated into a boolean value
        /// </summary>
        public Formula Formula { get; set; }

        /// <summary>
        /// The variables used by this criterion
        /// </summary>
        public IEnumerable<VariableRef> Variables => Formula.Variables;

        /// <summary>
        /// This is just a parameterless constructor for EF Core stuff
        /// </summary>
        public Criterion()
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
        public bool IsCriterionSastisfied(IEnumerable<ClinicalObservation> observations)
        {
            try
            {
                var result = Formula.ToExpression(observations).Evaluate();
                if (result is bool x)
                {
                    return x;
                }

                // The only way to reach this if the criterion is constructed not using
                // constructor
                throw new Exception($"Formula result type should be boolean, but get {result.GetType()}");
            }
            catch (ArgumentException)
            {
                // If the formula failed to construct/evaluate, then this criterion is not sastisfied
                return false;
            }
        }
    }
}
