using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;

namespace Respira.Clinical.Application.Features.Pathogens.CreatePathogen
{
    public class CreatePathogenMapper : ICreateMapper<Pathogen, CreatePathogenCommand>
    {
        public Pathogen ToModel(CreatePathogenCommand command)
        {
            return new Pathogen()
            {
                Name = command.Name,
                Description = command.Description,
                IsAtypical = command.IsAtypical
            };
        }
    }
}
