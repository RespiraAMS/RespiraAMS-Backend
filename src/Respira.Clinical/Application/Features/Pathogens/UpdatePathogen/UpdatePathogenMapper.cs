using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Pathogens.UpdatePathogen
{
    public class UpdatePathogenMapper : IUpdateMapper<Pathogen, UpdatePathogenCommand>
    {
        public Result MapModel(Pathogen model, UpdatePathogenCommand command)
        {
            model.Name = command.Name;
            model.Description = command.Description;
            model.IsAtypical = command.IsAtypical;
            model.UpdatedAt = DateTimeOffset.UtcNow;
            return Result.Success(ApplicationStatus.Success);
        }

        public Result MapModel(Pathogen model, UpdatePathogenCommand command, object? dependencies = null)
        {
            return MapModel(model, command);
        }
    }
}
