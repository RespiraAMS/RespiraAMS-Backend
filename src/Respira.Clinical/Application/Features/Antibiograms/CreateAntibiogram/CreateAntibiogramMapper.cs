using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Antibiograms.CreateAntibiogram
{
    public class CreateAntibiogramMapper : ICreateMapper<CreateAntibiogramCommand, Antibiogram>
    {
        public Result<Antibiogram> ToModel(CreateAntibiogramCommand command)
        {
            return Result<Antibiogram>.Success(ApplicationStatus.Success, new Antibiogram
            {
                PathogenId = command.PathogenId,
                MicCategory = command.MicCategory,
                Antibiotics = []
            });
        }

        public Result<Antibiogram> ToModel(CreateAntibiogramCommand command, object dependencies)
        {
            return ToModel(command);
        }
    }
}
