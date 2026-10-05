using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;

namespace Respira.Clinical.Application.Features.SuspectedCauses.CreateSuspectedCause
{
    public class CreateSuspectedCauseMapper : ICreateMapper<SuspectedCause, CreateSuspectedCauseCommand>
    {
        public SuspectedCause ToModel(CreateSuspectedCauseCommand command)
        {
            return new SuspectedCause
            {
                PathogenId = command.PathogenId,
                Severity = command.Severity,
                TreatmentSite = command.TreatmentSite,
            };
        }
    }
}
