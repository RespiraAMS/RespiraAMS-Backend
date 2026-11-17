using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.ClinicalVariables.SearchClinicalVariable
{
    public record SearchClinicalVariableQuery : IQuery
    {
        public required PaginationParam Param { get; set; }
        public required string SearchTerm { get; set; }
    }

    public record SearchClinicalVariableItem
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
        /// Clinical variable canonical unit
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

        /// <summary>
        /// Search rank (most relevant to search term)
        /// </summary>
        public required float Rank { get; set; }
    }
}
