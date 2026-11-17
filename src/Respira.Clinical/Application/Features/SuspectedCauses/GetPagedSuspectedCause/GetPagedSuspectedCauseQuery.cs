using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.SuspectedCauses.GetPagedSuspectedCause
{
    public record SuspectedCauseFilter
    {
        public Guid? PathogenId { get; set; }
        public Severity? Severity { get; set; }
        public TreatmentSite? TreatmentSite { get; set; }
    }

    public record GetPagedSuspectedCauseQuery : IQuery
    {
        public required PaginationParam Param { get; set; }
        public SuspectedCauseFilter? Filter { get; set; }
    }

    public record PagedSuspectedCauseItem
    {
        public required Guid Id { get; set; }
        public required Guid PathogenId { get; set; }
        public required string PathogenName { get; set; }
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
    }
}
