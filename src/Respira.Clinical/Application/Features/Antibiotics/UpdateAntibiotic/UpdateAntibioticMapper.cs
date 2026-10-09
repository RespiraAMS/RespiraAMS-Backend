using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Antibiotics.UpdateAntibiotic
{
    public class UpdateAntibioticMapper : IUpdateMapper<Antibiotic, UpdateAntibioticCommand>
    {
        public Result MapModel(Antibiotic model, UpdateAntibioticCommand command)
        {
            model.Name = command.Name;
            model.AntibioticGroupId = command.AntibioticGroupId;
            model.Classification = command.Classification;
            model.UpdatedAt = DateTimeOffset.UtcNow;
            return Result.Success(ApplicationStatus.Success);
        }

        public Result MapModel(Antibiotic model, UpdateAntibioticCommand command, object? dependencies = null)
        {
            return MapModel(model, command);
        }
    }
}
