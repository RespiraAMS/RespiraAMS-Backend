using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.Antibiograms.GetPagedAntibiogram
{
    public record AntibiogramFilter
    {
        /// <summary>
        /// Pathogen ID
        /// </summary>
        public Guid? PathogenId { get; set; }

        /// <summary>
        /// MIC category
        /// </summary>
        public MicCategory? MicCategory { get; set; }
    }

    public record GetPagedAntibiogramQuery : IQuery
    {
        /// <summary>
        /// Pagination parameter
        /// </summary>
        public required PaginationParam Param { get; set; }

        /// <summary>
        /// Antibiogram filter
        /// </summary>
        public AntibiogramFilter? Filter { get; set; }
    }

    public record AntibioticResult
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
        /// Antibiotic group ID
        /// </summary>
        public required Guid AntibioticGroupId { get; set; }

        /// <summary>
        /// Antibiotic group name
        /// </summary>
        public required string AntibioticGroupName { get; set; }

        /// <summary>
        /// Antibiotic AWaRe classification
        /// </summary>
        public required AwareClassification Classification { get; set; }
    }

    /// <summary>
    /// Pathogen result
    /// </summary>
    /// <param name="Id">Pathogen ID</param>
    /// <param name="Name">Pathogen name</param>
    public record PathogenResult(Guid Id, string Name);

    public record PagedAntibiogramItem
    {
        /// <summary>
        /// Antibiogram ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Pathogen of this antibiogram
        /// </summary>
        public required PathogenResult Pathogen { get; set; }

        /// <summary>
        /// MIC category
        /// </summary>
        public required MicCategory MicCategory { get; set; }

        /// <summary>
        /// List of antibiotics
        /// </summary>
        public required IEnumerable<AntibioticResult> Antibiotics { get; set; }
    }

}
