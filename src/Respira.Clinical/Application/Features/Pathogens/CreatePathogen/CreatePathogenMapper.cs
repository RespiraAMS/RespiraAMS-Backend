using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Pathogens.CreatePathogen
{
    public class CreatePathogenMapper : ICreateMapper<CreatePathogenCommand, Pathogen>
    {
        public Result<Pathogen> ToModel(CreatePathogenCommand command)
        {
            return Result<Pathogen>.Success(ApplicationStatus.Success, new Pathogen
            {
                Name = command.Name,
                Description = command.Description,
                IsAtypical = command.IsAtypical
            });
        }

        public Result<Pathogen> ToModel(CreatePathogenCommand command, object? dependencies = null)
        {
            return ToModel(command);
        }
    }
}
