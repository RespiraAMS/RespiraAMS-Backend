using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;

namespace Respira.Clinical.Application.Features.Antibiotics.AddDosage
{

    public class AddDosageMapper : ICreateMapper<Dosage, AddDosageCommand>
    {
        public Dosage ToModel(AddDosageCommand command)
        {
            return new Dosage
            {
                AntibioticId = command.AntibioticId,
                RouteOfAdministration = command.RouteOfAdministration,
                Dose = command.Dose,
                Crcl = command.Crcl
            };
        }
    }
}
