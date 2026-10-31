using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.TreatmentProtocols.GetPagedTreatmentProtocol
{
    public record TreatmentProtocolFilter
    {
        public Severity? Severity { get; set; }
        public TreatmentSite? TreatmentSite { get; set; }
        public List<Guid>? PathogenIds { get; set; }
        public List<Guid>? CriteriaIds { get; set; }
    }

    public record GetPagedTreatmentProtocolQuery : IQuery
    {
        public required PaginationParam Param { get; set; }
        public TreatmentProtocolFilter? Filter { get; set; }
    }

    public record AntibioticResult
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required Guid AntibioticGroupId { get; set; }
        public required string AntibioticGroupName { get; set; }
        public required AwareClassification Classification { get; set; }
    }

    public record PathogenResult(Guid Id, string Name);
    public record CriterionResult(Guid Id, string Name, string Formula);

    public record TreatmentProtocolItem
    {

        public required Guid Id { get; set; }
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
        public required IEnumerable<IEnumerable<AntibioticResult>> Treatments { get; set; }
        public required IEnumerable<PathogenResult> Pathogens { get; set; }
        public required IEnumerable<CriterionResult> Criteria { get; set; }
    }


}
