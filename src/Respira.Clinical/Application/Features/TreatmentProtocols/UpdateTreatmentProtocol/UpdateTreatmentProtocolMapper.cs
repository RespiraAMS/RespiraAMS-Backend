using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.TreatmentProtocols.UpdateTreatmentProtocol
{
    public class UpdateTreatmentProtocolMapper : IUpdateMapper<TreatmentProtocol, UpdateTreatmentProtocolCommand>
    {
        public Result MapModel(TreatmentProtocol model, UpdateTreatmentProtocolCommand protocolCommand)
        {
            model.Severity = protocolCommand.Severity;
            model.TreatmentSite = protocolCommand.TreatmentSite;
            model.UpdatedAt = DateTimeOffset.UtcNow;
            return Result.Success(ApplicationStatus.Success);
        }

        public Result MapModel(TreatmentProtocol model, UpdateTreatmentProtocolCommand protocolCommand, object dependencies)
        {
            return MapModel(model, protocolCommand);
        }
    }
}
