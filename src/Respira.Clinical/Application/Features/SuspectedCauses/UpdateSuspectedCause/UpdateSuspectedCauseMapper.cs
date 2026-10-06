using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.SuspectedCauses.UpdateSuspectedCause
{
    public class UpdateSuspectedCauseMapper : IUpdateMapper<SuspectedCause, UpdateSuspectedCauseCommand>
    {
        public Result MapModel(SuspectedCause model, UpdateSuspectedCauseCommand command)
        {
            model.PathogenId = command.PathogenId;
            model.Severity = command.Severity;
            model.TreatmentSite = command.TreatmentSite;
            model.UpdatedAt = DateTimeOffset.UtcNow;
            return Result.Success(ApplicationStatus.Success);
        }
    }
}
