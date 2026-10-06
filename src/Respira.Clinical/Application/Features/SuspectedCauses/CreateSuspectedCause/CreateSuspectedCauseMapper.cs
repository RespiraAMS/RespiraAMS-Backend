using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.SuspectedCauses.CreateSuspectedCause
{
    public class CreateSuspectedCauseMapper : ICreateMapper<CreateSuspectedCauseCommand, SuspectedCause>
    {
        public Result<SuspectedCause> ToModel(CreateSuspectedCauseCommand command)
        {
            return Result<SuspectedCause>.Success(ApplicationStatus.Success, new SuspectedCause
            {
                PathogenId = command.PathogenId,
                Severity = command.Severity,
                TreatmentSite = command.TreatmentSite,
            });
        }
    }
}
