using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Treatments.CreateTreatment
{
    public class CreateTreatmentMapper : ICreateMapper<CreateTreatmentCommand, Treatment>
    {
        public Result<Treatment> ToModel(CreateTreatmentCommand command)
        {
            return Result<Treatment>.Success(ApplicationStatus.Success, new Treatment
            {
                Severity = command.Severity,
                TreatmentSite = command.TreatmentSite,
                // Leave the collection empty for the UpdateRelations to work
                Medicines = [],
                Pathogens = [],
                Criteria = [],
            });
        }

        public Result<Treatment> ToModel(CreateTreatmentCommand command, object? dependencies = null)
        {
            return ToModel(command);
        }
    }
}
