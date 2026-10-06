using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Antibiotics.UpdateDosage
{
    public class UpdateDosageMapper : IUpdateMapper<Dosage, UpdateDosageCommand>
    {
        public Result MapModel(Dosage model, UpdateDosageCommand command)
        {
            model.RouteOfAdministration = command.RouteOfAdministration;
            model.Dose = command.Dose;
            model.Crcl = command.Crcl;
            model.UpdatedAt = DateTimeOffset.UtcNow;
            return Result.Success(ApplicationStatus.Success);
        }
    }
}
