using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Models;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Domain.Entities
{
    /// <summary>
    /// This is the class represent a clinical variable, like BUN, Systoloc blood pressure,...
    /// </summary>
    public abstract class ClinicalVariable : Base
    {
        /// <summary>
        /// Clinical variable code. Preferably follow LOINC code
        /// </summary>
        public required string Code { get; set; }

        /// <summary>
        /// Clinical variable name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Clinical variable description
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Clinical variable type
        /// </summary>
        public abstract ClinicalValueType ValueType { get; }

        /// <summary>
        /// Actual unit used by the engine
        /// </summary>
        public string? CanonicalUnit { get; set; }

        public abstract bool IsValidValue(object? value);
    }

    public class BooleanClinicalVariable : ClinicalVariable
    {
        public override ClinicalValueType ValueType => ClinicalValueType.Boolean;

        public override bool IsValidValue(object? value)
        {
            return value is bool;
        }
    }

    public class NumericClinicalVariable : ClinicalVariable
    {
        public Range AcceptedRange { get; set; } = null!;

        public override ClinicalValueType ValueType => ClinicalValueType.Numeric;

        public override bool IsValidValue(object? value)
        {
            return value is decimal dvalue && AcceptedRange.IsInRange(dvalue);
        }
    }

    public class CategoricalClinicalVariable : ClinicalVariable
    {
        public List<string> AcceptedValues { get; set; } = [];

        public override ClinicalValueType ValueType => ClinicalValueType.Categorical;

        private string SanitizeCategory(string value)
        {
            // Trim space and normalize to upper case
            var sanitized = value.Trim().ToUpper();
            // Remove all in-between spaces with a single underscore
            return string.Join("_", sanitized.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        public CategoricalClinicalVariable(List<string> acceptedValues)
        {
            AcceptedValues.AddRange(acceptedValues.Select(SanitizeCategory));
        }

        public override bool IsValidValue(object? value)
        {
            return value is string svalue && AcceptedValues.Contains(SanitizeCategory(svalue));
        }
    }
}
