using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.Antibiotics.SearchAntibiotic
{
    public record SearchAntibioticQuery : IQuery
    {
        public required PaginationParam Param { get; set; }
        public required string SearchTerm { get; init; }
    }

    public record AntibioticGroupResult
    {
        /// <summary>
        /// Antibiotic group ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Antibiotic group name
        /// </summary>
        public required string Name { get; set; }
    }

    public record SearchAntibioticItem
    {
        /// <summary>
        /// Antibiotic ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Antibiotic name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Antibiotic group
        /// </summary>
        public required AntibioticGroupResult AntibioticGroup { get; set; }

        /// <summary>
        /// Antibiotic WHO's AWaRe category
        /// </summary>
        public required AwareClassification Classification { get; set; }

        /// <summary>
        /// Search rank (most relevant to search term)
        /// </summary>
        public required float Rank { get; set; }
    }
}
