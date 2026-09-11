using Respira.Domain.Enums;
using Respira.ServiceDefaults.Models;

namespace Respira.Domain.Entities
{
    /// <summary>
    /// This is the class represent a clinical variable, like BUN, Systoloc blood pressure,...
    /// </summary>
    public class ClinicalVariable : Base
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
        public required ClinicalValueType ValueType { get; set; }

        /// <summary>
        /// Actual unit used by the engine
        /// </summary>
        public string? CanonicalUnit { get; set; }
    }
}
