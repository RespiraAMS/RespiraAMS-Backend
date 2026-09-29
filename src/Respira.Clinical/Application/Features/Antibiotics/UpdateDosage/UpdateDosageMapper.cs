using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;

namespace Respira.Clinical.Application.Features.Antibiotics.UpdateDosage
{
    public class UpdateDosageMapper : IUpdateMapper<Dosage, UpdateDosageCommand>
    {
        public void MapModel(Dosage model, UpdateDosageCommand command)
        {
            model.RouteOfAdministration = command.RouteOfAdministration;
            model.Dose = command.Dose;
            model.Crcl = command.Crcl;
            model.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
