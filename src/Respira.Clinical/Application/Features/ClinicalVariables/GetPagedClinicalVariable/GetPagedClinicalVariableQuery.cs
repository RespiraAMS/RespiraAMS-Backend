using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.ClinicalVariables.GetPagedClinicalVariable
{
    public record ClinicalVariableFilter
    {
        /// <summary>
        /// Clinical variable name
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Clinical variable code
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Clinical variable is required flag
        /// </summary>
        public bool? IsRequired { get; set; }

        /// <summary>
        /// Clinical variable value type
        /// </summary>
        public ClinicalValueType? ValueType { get; set; }

        /// <summary>
        /// Clinical variable category
        /// </summary>
        public ClinicalVariableCategory? Category { get; set; }
    }

    public record GetPagedClinicalVariableQuery : IQuery
    {
        /// <summary>
        /// Pagination parameter
        /// </summary>
        public required PaginationParam Param { get; set; }

        /// <summary>
        /// Clinical variable filter
        /// </summary>
        public ClinicalVariableFilter? Filter { get; set; }
    }

    public record PagedClinicalVariableItem
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
        /// Clinical variable is required flag
        /// </summary>
        public required bool IsRequired { get; set; }

        /// <summary>
        /// Clinical variable description
        /// </summary>
        public string? CanonicalUnit { get; set; }

        /// <summary>
        /// Clinical variable value type
        /// </summary>
        public required ClinicalValueType ValueType { get; set; }

        /// <summary>
        /// Clinical variable category
        /// </summary>
        public required ClinicalVariableCategory Category { get; set; }
    }
}
