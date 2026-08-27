using Application.Contracts.Mappers;
using Application.Features.Antibiotics.CreateAntibiotic;
using Domain.Enums;
using Domain.Models;

namespace Application.Test.Features.Antibiotics.CreateAntibiotic;

public class CreateAntibioticMapperTest
{
    private readonly ICreateMapper<Antibiotic, CreateAntibioticCommand> _mapper =
        new CreateAntibioticMapper();

    # region Happy path

    [Fact]
    public void ToModel_CreatesAntibioticWithStandardDosage_Success()
    {
        var groupId = Guid.CreateVersion7();
        var command = new CreateAntibioticCommand
        {
            Name = "Amoxicillin",
            AntibioticGroupId = groupId,
            Classification = AwareClassification.Access,
            RouteOfAdministration = RouteOfAdministration.Oral,
            StandardDose = "500 mg orally every 8 hours",
        };

        var model = _mapper.ToModel(command);

        Assert.Equal("Amoxicillin", model.Name);
        Assert.Equal(groupId, model.AntibioticGroupId);
        Assert.Equal(AwareClassification.Access, model.Classification);
        // Base generates the ID so the handler can return it right after saving
        Assert.NotEqual(Guid.Empty, model.Id);

        // The mapper must build exactly one standard dose (no CrCl range)
        var dosage = Assert.Single(model.Dosages);
        Assert.Equal(model.Id, dosage.AntibioticId);
        Assert.Equal(command.RouteOfAdministration, dosage.RouteOfAdministration);
        Assert.Equal(command.StandardDose, dosage.Dose);
        Assert.Null(dosage.Crcl);
        Assert.Contains(dosage.Id, model.DosageIds);
    }

    # endregion
}
