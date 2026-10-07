using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Application.Features.ClinicalVariables.UpdateClinicalVariable
{
    public record UpdateClinicalVariableCommand : ICommand
    {
        /// <summary>
        /// Clinical variable ID
        /// </summary>
        public required Guid Id { get; set; }

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
        /// Clinical variable type. Update CANNOT change the value type,
        /// this value is simply needed for mapping and validation
        /// </summary>
        public required ClinicalValueType ValueType { get; set; }

        /// <summary>
        /// Actual unit used by the engine
        /// </summary>
        public string? CanonicalUnit { get; set; }

        /// <summary>
        /// Clinical variable that is required when diagnosis or not
        /// </summary>
        public required bool IsRequired { get; set; }

        /// <summary>
        /// Clinical variable category
        /// </summary>
        public required ClinicalVariableCategory Category { get; set; }

        /// <summary>
        /// Prerequisite formula for this variable. For example,
        /// for a PREGNANT-OR-LACTATING variable to be true, then
        /// FEMALE must be true
        /// </summary>
        public FormulaDto? Prerequisite { get; set; }

        /// <summary>
        /// The numeric range that the value can be. Note that, the unit of this range
        /// will be forced to use the canonical unit of the variable, regardless of what
        /// you set
        /// </summary>
        public Range? AcceptedRange { get; set; }

        /// <summary>
        /// The numeric range that the value should be normally.
        /// If value inputted outside of this range, but still within <see cref="AcceptedRange"/>,
        /// it will still be accepted, but the client should be warned. Note that, the unit of this range
        /// will be forced to use the canonical unit of the variable, regardless of what
        /// </summary>
        public Range? NormalRange { get; set; }

        /// <summary>
        /// The categorical values that the value can be
        /// </summary>
        public List<string> AcceptedValues { get; set; } = [];
    }
}
