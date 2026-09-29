using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;

namespace Respira.Clinical.Application.Features.Antibiotics.UpdateAntibiotic
{
    public class UpdateAntibioticMapper : IUpdateMapper<Antibiotic, UpdateAntibioticCommand>
    {
        public void MapModel(Antibiotic model, UpdateAntibioticCommand command)
        {
            model.Name = command.Name;
            model.AntibioticGroupId = command.AntibioticGroupId;
            model.Classification = command.Classification;
            model.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
