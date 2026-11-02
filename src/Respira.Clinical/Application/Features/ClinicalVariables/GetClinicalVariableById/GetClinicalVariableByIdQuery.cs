using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Application.Features.ClinicalVariables.GetClinicalVariableById
{
    public record GetClinicalVariableByIdQuery : IQuery
    {
        /// <summary>
        /// Clinical variable ID
        /// </summary>
        public required Guid Id { get; set; }
    }

    public record ClinicalVariableResult
    {
        /// <summary>
        /// Clinical variable ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Clinical variable name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Clinical variable code
        /// </summary>
        public required string Code { get; set; }

        /// <summary>
        /// Clinical variable description
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Clinical variable value type
        /// </summary>
        public required ClinicalValueType ValueType { get; set; }

        /// <summary>
        /// Clinical variable canonical unit
        /// </summary>
        public string? CanonicalUnit { get; set; }

        /// <summary>
        /// Is clinical variable required
        /// </summary>
        public required bool IsRequired { get; set; }

        /// <summary>
        /// Clinical variable category
        /// </summary>
        public required ClinicalVariableCategory Category { get; set; }

        /// <summary>
        /// Clinical variable prerequisite
        /// </summary>
        public string? Prerequisite { get; set; } // Store as string representation of Formula

        public FormulaDto? PrerequisiteFormula { get; set; }

        /// <summary>
        /// Clinical variable accepted range. Only exists if <see cref="ValueType"/> is <see cref="ClinicalValueType.Numeric"/>
        /// </summary>
        public Range? AcceptedRange { get; set; }

        /// <summary>
        /// Clinical variable normal range. Only exists if <see cref="ValueType"/> is <see cref="ClinicalValueType.Numeric"/>
        /// </summary>
        public Range? NormalRange { get; set; }

        /// <summary>
        /// Clinical variable accepted values. Only exists if <see cref="ValueType"/> is <see cref="ClinicalValueType.Categorical"/>
        /// </summary>
        public List<string>? AcceptedValues { get; set; }
    }
}
