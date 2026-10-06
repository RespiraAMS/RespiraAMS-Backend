using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Antibiotics.AddDosage
{

    public class AddDosageMapper : ICreateMapper<AddDosageCommand, Dosage>
    {
        public Result<Dosage> ToModel(AddDosageCommand command)
        {
            return Result<Dosage>.Success(ApplicationStatus.Success, new Dosage
            {
                AntibioticId = command.AntibioticId,
                RouteOfAdministration = command.RouteOfAdministration,
                Dose = command.Dose,
                Crcl = command.Crcl
            });
        }
    }
}
