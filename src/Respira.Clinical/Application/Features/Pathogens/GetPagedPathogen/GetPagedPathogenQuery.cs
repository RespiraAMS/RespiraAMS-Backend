using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.Pathogens.GetPagedPathogen
{
    public record PathogenFilter
    {
        /// <summary>
        /// Pathogen name
        /// </summary>
        public string? Name { get; set; }
    }

    public record GetPagedPathogenQuery : IQuery
    {
        /// <summary>
        /// Pagination param
        /// </summary>
        public required PaginationParam Param { get; set; } = null!;

        /// <summary>
        /// Pathogen filter
        /// </summary>
        public PathogenFilter? Filter { get; set; }
    }

    public record PagedPathogenItem
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
    }
}
