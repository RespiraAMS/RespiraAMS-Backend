using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;

namespace Respira.Clinical.Domain.Models
{
    /// <summary>
    /// Lightweight reference to a clinical variable, stored inside a formula's JSONB payload.
    /// <para>
    /// Deliberately minimal (<c>Id</c>, <c>Code</c>, <c>ValueType</c>): a formula only ever needs
    /// the code to match observations and the value type to derive its result type. Keeping the
    /// full <see cref="ClinicalVariable"/> entity out of the document means changes to the
    /// clinical_variables table can never break deserialization of stored formulas.
    /// </para>
    /// </summary>
    public record VariableRef(Guid Id, string Code, ClinicalValueType ValueType)
    {
        /// <summary>
        /// Lets formula construction sites keep passing the entity directly:
        /// <c>new VariableFormula(variable)</c>.
        /// </summary>
        public static implicit operator VariableRef(ClinicalVariable variable)
        {
            return new(variable.Id, variable.Code, variable.ValueType);
        }
    }
}
