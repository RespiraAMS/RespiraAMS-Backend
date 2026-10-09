using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Treatments.UpdateTreatment
{
    public class UpdateTreatmentMapper : IUpdateMapper<Treatment, UpdateTreatmentCommand>
    {
        public Result MapModel(Treatment model, UpdateTreatmentCommand command)
        {
            model.Severity = command.Severity;
            model.TreatmentSite = command.TreatmentSite;
            model.UpdatedAt = DateTimeOffset.UtcNow;
            return Result.Success(ApplicationStatus.Success);
        }

        public Result MapModel(Treatment model, UpdateTreatmentCommand command, object? dependencies = null)
        {
            return MapModel(model, command);
        }
    }
}
