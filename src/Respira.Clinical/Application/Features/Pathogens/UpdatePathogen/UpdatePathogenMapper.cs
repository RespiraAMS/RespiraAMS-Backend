using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;

namespace Respira.Clinical.Application.Features.Pathogens.UpdatePathogen
{
    public class UpdatePathogenMapper : IUpdateMapper<Pathogen, UpdatePathogenCommand>
    {
        public void MapModel(Pathogen model, UpdatePathogenCommand command)
        {
            model.Name = command.Name;
            model.Description = command.Description;
            model.IsAtypical = command.IsAtypical;
            model.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
