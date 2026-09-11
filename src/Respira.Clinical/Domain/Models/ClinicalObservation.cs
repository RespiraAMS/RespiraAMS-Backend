using Respira.Domain.Entities;
using Respira.Domain.Enums;

namespace Respira.Domain.Models
{
    /// <summary>
    /// This is the class represent a clinical observation, like systoloc blood pressure, BUN,...
    /// </summary>
    public record ClinicalObservation
    {
        /// <summary>
        /// Clinical variable that the observation is about
        /// </summary>
        public ClinicalVariable Variable { get; set; }

        /// <summary>
        /// Clinical observation value. This would be not null if the variable is of type Numeric
        /// </summary>
        public decimal? NumericValue { get; set; }

        /// <summary>
        /// Clinical observation value. This would be not null if the variable is of type Boolean
        /// </summary>
        public bool? BooleanValue { get; set; }

        /// <summary>
        /// Constructor for ClinicalObservation with numeric value
        /// </summary>
        /// <param name="variable">Clinical variable</param>
        /// <param name="numericValue">Observed value</param>
        /// <exception cref="ArgumentException">Throw if the variable is of type Boolean</exception>
        public ClinicalObservation(ClinicalVariable variable, decimal numericValue)
        {
            if (variable.ValueType != ClinicalValueType.Numeric)
            {
                throw new ArgumentException("Cannot construct a numeric clinical observation: variable is not Numeric");
            }

            Variable = variable;
            NumericValue = numericValue;
            BooleanValue = null;
        }

        /// <summary>
        /// Constructor for ClinicalObservation with boolean value
        /// </summary>
        /// <param name="variable">Clinical variable</param>
        /// <param name="booleanValue">Observed value</param>
        /// <exception cref="ArgumentException">Throw if the variable is of type Numeric</exception>
        public ClinicalObservation(ClinicalVariable variable, bool booleanValue)
        {
            if (variable.ValueType != ClinicalValueType.Boolean)
            {
                throw new ArgumentException("Cannot construct a numeric clinical observation: variable is not Boolean");
            }

            Variable = variable;
            NumericValue = null;
            BooleanValue = booleanValue;
        }
    }
}
