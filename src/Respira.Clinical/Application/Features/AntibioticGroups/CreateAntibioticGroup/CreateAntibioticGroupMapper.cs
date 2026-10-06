using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.AntibioticGroups.CreateAntibioticGroup
{
    public class CreateAntibioticGroupMapper : ICreateMapper<CreateAntibioticGroupCommand, AntibioticGroup>
    {
        public Result<AntibioticGroup> ToModel(CreateAntibioticGroupCommand command)
        {
            return Result<AntibioticGroup>.Success(ApplicationStatus.Success, new AntibioticGroup
            {
                Name = command.Name,
                Description = command.Description,
                ParentId = command.ParentId
            });
        }
    }
}
