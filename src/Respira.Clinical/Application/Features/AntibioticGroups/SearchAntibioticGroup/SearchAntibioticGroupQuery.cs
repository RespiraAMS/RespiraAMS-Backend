using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.AntibioticGroups.SearchAntibioticGroup
{
    public record SearchAntibioticGroupQuery : IQuery
    {
        public required PaginationParam Param { get; set; }
        public required string SearchTerm { get; set; }
    }

    public record SearchAntibioticGroupItem
    {
        /// <summary>
        /// Antibiotic group ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Antibiotic group name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Antibiotic group parent ID
        /// </summary>
        public required Guid? ParentId { get; set; }

        /// <summary>
        /// Antibiotic group parent name
        /// </summary>
        public required string? ParentName { get; set; }

        /// <summary>
        /// Antibiotic group description
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Search rank (most relevant to search term)
        /// </summary>
        public required float Rank { get; set; }
    }
}
