using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.TreatmentProtocols.CreateTreatmentProtocol
{
    public class CreateTreatmentProtocolMapper : ICreateMapper<CreateTreatmentProtocolCommand, TreatmentProtocol>
    {
        public Result<TreatmentProtocol> ToModel(CreateTreatmentProtocolCommand command)
        {
            return Result<TreatmentProtocol>.Success(ApplicationStatus.Success, new TreatmentProtocol
            {
                Severity = command.Severity,
                TreatmentSite = command.TreatmentSite,
                Publisher = command.Publisher,
                Version = command.Version,
                IsApplied = command.IsApplied,
                // Leave the collection empty for the UpdateRelations to work
                Treatments = [],
                Pathogens = [],
                Criteria = [],
            });
        }

        public Result<TreatmentProtocol> ToModel(CreateTreatmentProtocolCommand command, object dependencies)
        {
            return ToModel(command);
        }
    }
}
