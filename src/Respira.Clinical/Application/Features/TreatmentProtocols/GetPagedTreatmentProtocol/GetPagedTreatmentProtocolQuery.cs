using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.TreatmentProtocols.GetPagedTreatmentProtocol
{
    public record TreatmentProtocolFilter
    {
        /// <summary>
        /// Treatment protocol's severity
        /// </summary>
        public Severity? Severity { get; set; }

        /// <summary>
        /// Treatment protocol's treatment site
        /// </summary>
        public TreatmentSite? TreatmentSite { get; set; }

        /// <summary>
        /// List of pathogen IDs
        /// </summary>
        public List<Guid>? PathogenIds { get; set; }

        /// <summary>
        /// List of criterion IDs
        /// </summary>
        public List<Guid>? CriteriaIds { get; set; }
    }

    public record GetPagedTreatmentProtocolQuery : IQuery
    {
        /// <summary>
        /// Pagination parameter
        /// </summary>
        public required PaginationParam Param { get; set; }

        /// <summary>
        /// Treatment protocol filter
        /// </summary>
        public TreatmentProtocolFilter? Filter { get; set; }
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

    /// <summary>
    /// Criterion result
    /// </summary>
    /// <param name="Id">Criterion ID</param>
    /// <param name="Name">Criterion name</param>
    /// <param name="Formula">Criterion formula</param>
    public record CriterionResult(Guid Id, string Name, string Formula);

    public record TreatmentProtocolItem
    {
        /// <summary>
        /// Treatment protocol ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Treatment protocol's severity
        /// </summary>
        public required Severity Severity { get; set; }

        /// <summary>
        /// Treatment protocol's treatment site
        /// </summary>
        public required TreatmentSite TreatmentSite { get; set; }

        /// <summary>
        /// Treatment protocol's treatments
        /// </summary>
        public required IEnumerable<IEnumerable<AntibioticResult>> Treatments { get; set; }

        /// <summary>
        /// Treatment protocol's infected pathogen list
        /// </summary>
        public required IEnumerable<PathogenResult> Pathogens { get; set; }

        /// <summary>
        /// Treatment protocol's criteria list
        /// </summary>
        public required IEnumerable<CriterionResult> Criteria { get; set; }

        /// <summary>
        /// Version of the metric
        /// </summary>
        public required string Version { get; set; }

        /// <summary>
        /// Metric publisher
        /// </summary>
        public required string Publisher { get; set; }

        /// <summary>
        /// Metric status, is applied or not
        /// </summary>
        public required bool IsApplied { get; set; }
    }
}
