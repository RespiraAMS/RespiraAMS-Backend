using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.TreatmentProtocols.CreateTreatmentProtocol
{
    public class CreateTreatmentProtocolMapper : ICreateMapper<CreateTreatmentProtocolCommand, TreatmentProtocol>
    {
        public Result<TreatmentProtocol> ToModel(CreateTreatmentProtocolCommand protocolCommand)
        {
            return Result<TreatmentProtocol>.Success(ApplicationStatus.Success, new TreatmentProtocol
            {
                Severity = protocolCommand.Severity,
                TreatmentSite = protocolCommand.TreatmentSite,
                // Leave the collection empty for the UpdateRelations to work
                Treatments = [],
                Pathogens = [],
                Criteria = [],
            });
        }

        public Result<TreatmentProtocol> ToModel(CreateTreatmentProtocolCommand protocolCommand, object dependencies)
        {
            return ToModel(protocolCommand);
        }
    }
}
