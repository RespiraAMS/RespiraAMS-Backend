using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;

namespace Respira.Clinical.Application.Features.SuspectedCauses.UpdateSuspectedCause
{
    public class UpdateSuspectedCauseMapper : IUpdateMapper<SuspectedCause, UpdateSuspectedCauseCommand>
    {
        public void MapModel(SuspectedCause model, UpdateSuspectedCauseCommand command)
        {
            model.PathogenId = command.PathogenId;
            model.Severity = command.Severity;
            model.TreatmentSite = command.TreatmentSite;
            model.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
