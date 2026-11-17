using Respira.Clinical.Application.Features.SuspectedCauses.GetPagedSuspectedCause;
using Respira.Clinical.Application.Features.SuspectedCauses.UpdateSuspectedCause;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.API.Dtos
{
    public record GetPagedSuspectedCauseRequestDto
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
        /// Pathogen ID
        /// </summary>
        public Guid? PathogenId { get; set; }

        /// <summary>
        /// Severity assigned to the suspected cause
        /// </summary>
        public Severity? Severity { get; set; }

        /// <summary>
        /// Treatment site assigned to the suspected cause
        /// </summary>
        public TreatmentSite? TreatmentSite { get; set; }
        public GetPagedSuspectedCauseQuery ToQuery()
        {
            return new GetPagedSuspectedCauseQuery
            {
                Param = new PaginationParam
                {
                    Page = Page,
                    Size = Size,
                },
                Filter = new SuspectedCauseFilter
                {
                    PathogenId = PathogenId,
                    Severity = Severity,
                    TreatmentSite = TreatmentSite
                }
            };
        }
    }

    public record UpdateSuspectedCauseRequestDto
    {
        /// <summary>
        /// Pathogen ID
        /// </summary>
        public required Guid PathogenId { get; set; }

        /// <summary>
        /// Severity
        /// </summary>
        public required Severity Severity { get; set; }

        /// <summary>
        /// Treatment site
        /// </summary>
        public required TreatmentSite TreatmentSite { get; set; }

        public UpdateSuspectedCauseCommand ToCommand(Guid id)
        {
            return new UpdateSuspectedCauseCommand
            {
                Id = id,
                PathogenId = PathogenId,
                Severity = Severity,
                TreatmentSite = TreatmentSite
            };
        }
    }
}
