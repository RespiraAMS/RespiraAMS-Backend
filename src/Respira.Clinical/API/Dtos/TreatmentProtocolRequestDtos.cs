using Respira.Clinical.Application.Features.TreatmentProtocols.GetPagedTreatmentProtocol;
using Respira.Clinical.Application.Features.TreatmentProtocols.UpdateTreatmentProtocol;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.API.Dtos
{
    public record GetPagedTreatmentProtocolRequestDto
    {
        /// <summary>
        /// Pagination parameter: page index (1-based)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Pagination parameter: page size
        /// </summary>
        public int Size { get; set; } = 10;

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

        public GetPagedTreatmentProtocolQuery ToQuery()
        {
            return new GetPagedTreatmentProtocolQuery
            {
                Param = new PaginationParam
                {
                    Page = Page,
                    Size = Size,
                },
                Filter = new TreatmentProtocolFilter
                {
                    Severity = Severity,
                    TreatmentSite = TreatmentSite,
                    PathogenIds = PathogenIds,
                    CriteriaIds = CriteriaIds
                }
            };
        }
    }

    public record UpdateTreatmentProtocolRequestDto
    {
        /// <summary>
        /// Severity of the treatment
        /// </summary>
        public required Severity Severity { get; set; }

        /// <summary>
        /// TreatmentProtocol site of the treatment
        /// </summary>
        public required TreatmentSite TreatmentSite { get; set; }

        /// <summary>
        /// List of medicine compositions. Each sub-list represents a medicine composition,
        /// where it contains one or multiple antibiotic IDs
        /// </summary>
        public List<List<Guid>> Medicines { get; set; } = [];

        /// <summary>
        /// List of pathogen IDs associated with the treatment
        /// </summary>
        public List<Guid> Pathogens { get; set; } = [];

        /// <summary>
        /// List of criteria IDs associated with the treatment
        /// </summary>
        public List<Guid> Criteria { get; set; } = [];

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

        public UpdateTreatmentProtocolCommand ToCommand(Guid id)
        {
            return new UpdateTreatmentProtocolCommand
            {
                Id = id,
                Severity = Severity,
                TreatmentSite = TreatmentSite,
                Medicines = Medicines,
                Pathogens = Pathogens,
                Criteria = Criteria,
                Publisher = Publisher,
                Version = Version,
                IsApplied = IsApplied,
            };
        }
    }
}
