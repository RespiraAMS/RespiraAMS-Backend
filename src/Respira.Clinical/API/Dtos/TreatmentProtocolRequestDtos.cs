using Respira.Clinical.Application.Features.TreatmentProtocols.GetPagedTreatmentProtocol;
using Respira.Clinical.Application.Features.TreatmentProtocols.UpdateTreatmentProtocol;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.API.Dtos
{
    public record GetPagedTreatmentProtocolRequestDto
    {
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
        public Severity? Severity { get; set; }
        public TreatmentSite? TreatmentSite { get; set; }
        public List<Guid>? PathogenIds { get; set; }
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
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
        public List<List<Guid>> Medicines { get; set; } = [];
        public List<Guid> Pathogens { get; set; } = [];
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
