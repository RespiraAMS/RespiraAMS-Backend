using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.TreatmentProtocols.UpdateTreatmentProtocol
{
    public class UpdateTreatmentProtocolMapper : IUpdateMapper<TreatmentProtocol, UpdateTreatmentProtocolCommand>
    {
        public Result MapModel(TreatmentProtocol model, UpdateTreatmentProtocolCommand command)
        {
            model.Severity = command.Severity;
            model.TreatmentSite = command.TreatmentSite;
            model.Publisher = command.Publisher;
            model.Version = command.Version;
            model.IsApplied = command.IsApplied;
            model.UpdatedAt = DateTimeOffset.UtcNow;
            return Result.Success(ApplicationStatus.Success);
        }

        public Result MapModel(TreatmentProtocol model, UpdateTreatmentProtocolCommand command, object dependencies)
        {
            return MapModel(model, command);
        }
    }
}
