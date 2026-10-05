using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;

namespace Respira.Clinical.Application.Features.AntibioticGroups.CreateAntibioticGroup
{
    public class CreateAntibioticGroupMapper : ICreateMapper<AntibioticGroup, CreateAntibioticGroupCommand>
    {
        public AntibioticGroup ToModel(CreateAntibioticGroupCommand command)
        {
            return new AntibioticGroup
            {
                Name = command.Name,
                Description = command.Description,
                ParentId = command.ParentId
            };
        }
    }
}
