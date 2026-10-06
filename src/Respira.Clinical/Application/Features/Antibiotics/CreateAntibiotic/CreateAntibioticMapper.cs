using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Antibiotics.CreateAntibiotic
{
    public class CreateAntibioticMapper : ICreateMapper<CreateAntibioticCommand, Antibiotic>
    {
        public Result<Antibiotic> ToModel(CreateAntibioticCommand command)
        {
            // Create antibiotic
            var antibiotic = new Antibiotic
            {
                Name = command.Name,
                AntibioticGroupId = command.AntibioticGroupId,
                Classification = command.Classification,
            };

            // Create standard dose
            var standardDose = new Dosage
            {
                AntibioticId = antibiotic.Id,
                RouteOfAdministration = command.RouteOfAdministration,
                Dose = command.StandardDose,
                Crcl = null
            };

            // Add standard dose into antibiotic
            antibiotic.Dosages.Add(standardDose);
            return Result<Antibiotic>.Success(ApplicationStatus.Success, antibiotic);
        }
    }
}
