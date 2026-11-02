using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Antibiograms.UpdateAntibiogram
{
    public class UpdateAntibiogramMapper : IUpdateMapper<Antibiogram, UpdateAntibiogramCommand>
    {
        public Result MapModel(Antibiogram model, UpdateAntibiogramCommand command)
        {
            model.PathogenId = command.PathogenId;
            model.MicCategory = command.MicCategory;
            model.UpdatedAt = DateTimeOffset.UtcNow;
            return Result.Success(ApplicationStatus.Success);
        }

        public Result MapModel(Antibiogram model, UpdateAntibiogramCommand command, object dependencies)
        {
            return MapModel(model, command);
        }
    }
}
