using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.AntibioticGroups.UpdateAntibioticGroup
{
    public class UpdateAntibioticGroupMapper : IUpdateMapper<AntibioticGroup, UpdateAntibioticGroupCommand>
    {
        public Result MapModel(AntibioticGroup model, UpdateAntibioticGroupCommand command)
        {
            model.Name = command.Name;
            model.Description = command.Description;
            model.ParentId = command.ParentId;
            model.UpdatedAt = DateTimeOffset.UtcNow;
            return Result.Success(ApplicationStatus.Success);
        }

        public Result MapModel(AntibioticGroup model, UpdateAntibioticGroupCommand command, object? dependencies = null)
        {
            return MapModel(model, command);
        }
    }
}
