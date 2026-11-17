using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.Pathogens.SearchPathogen
{
    public record SearchPathogenQuery : IQuery
    {
        public required PaginationParam Param { get; set; }
        public required string SearchTerm { get; set; }
    }

    public record SearchPathogenItem
    {
        /// <summary>
        /// Pathogen ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Pathogen name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Pathogen description
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Boolean flag to indicate if the pathogen is atypical.
        /// </summary>
        public required bool IsAtypical { get; set; }

        /// <summary>
        /// Search rank (most relevant to search term)
        /// </summary>
        public required float Rank { get; set; }
    }
}
