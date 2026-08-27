using Application.Contracts.Mappers;
using Application.Features.EmpiricTreatmentProtocols.CreateEmpiricTreatmentProtocol;
using Domain.Enums;
using Domain.Models;

namespace Application.Test.Features.EmpiricTreatmentProtocols.CreateEmpiricTreatmentProtocol;

public class CreateEmpiricTreatmentProtocolMapperTest
{
    private readonly ICreateMapper<EmpiricTreatmentProtocol, CreateEmpiricTreatmentProtocolCommand> _mapper =
        new CreateEmpiricTreatmentProtocolMapper();

    # region Happy path

    [Fact]
    public void ToModel_WithSpecialInfection_MapsAllScalarFields()
    {
        var diseaseId = Guid.CreateVersion7();
        var specialInfectionId = Guid.CreateVersion7();
        var command = new CreateEmpiricTreatmentProtocolCommand
        {
            Name = "WHO 2023 CAP Guidance",
            Issuer = "World Health Organization",
            IssueDate = new DateOnly(2023, 11, 1),
            Version = 2,
            DiseaseId = diseaseId,
            Severity = Severity.Severe,
            TreatmentSite = TreatmentSite.IntensiveCareUnit,
            SpecialInfectionId = specialInfectionId,
            OtherCriteriaIds = [Guid.CreateVersion7()],
            MedicineIds = [Guid.CreateVersion7(), Guid.CreateVersion7()],
        };

        var model = _mapper.ToModel(command);

        Assert.Equal(command.Name, model.Name);
        Assert.Equal(command.Issuer, model.Issuer);
        Assert.Equal(command.IssueDate, model.IssueDate);
        Assert.Equal(command.Version, model.Version);
        Assert.Equal(diseaseId, model.DiseaseId);
        Assert.Equal(Severity.Severe, model.Severity);
        Assert.Equal(TreatmentSite.IntensiveCareUnit, model.TreatmentSite);
        Assert.Equal(specialInfectionId, model.SpecialInfectionId);

        // Base generates the ID so the handler can return it right after saving
        Assert.NotEqual(Guid.Empty, model.Id);
    }

    [Fact]
    public void ToModel_WithoutSpecialInfection_KeepsNullSpecialInfectionId()
    {
        var command = new CreateEmpiricTreatmentProtocolCommand
        {
            Name = "Vietnam MOH 2023 Basic CAP Protocol",
            Issuer = "Vietnam Ministry of Health",
            IssueDate = new DateOnly(2023, 5, 15),
            Version = 1,
            DiseaseId = Guid.CreateVersion7(),
            Severity = Severity.Mild,
            TreatmentSite = TreatmentSite.Outpatient,
            SpecialInfectionId = null,
            OtherCriteriaIds = [],
            MedicineIds = [Guid.CreateVersion7()],
        };

        var model = _mapper.ToModel(command);

        Assert.Equal(command.Name, model.Name);
        Assert.Equal(command.Issuer, model.Issuer);
        Assert.Equal(command.IssueDate, model.IssueDate);
        Assert.Equal(command.Version, model.Version);
        Assert.Equal(Severity.Mild, model.Severity);
        Assert.Equal(TreatmentSite.Outpatient, model.TreatmentSite);
        Assert.Null(model.SpecialInfectionId);
    }

    [Fact]
    public void ToModel_IgnoresRelationIdLists()
    {
        var command = new CreateEmpiricTreatmentProtocolCommand
        {
            Name = "IDSA/ATS 2024 CAP Empiric Guidance",
            Issuer = "Infectious Diseases Society of America",
            IssueDate = new DateOnly(2024, 8, 1),
            Version = 1,
            DiseaseId = Guid.CreateVersion7(),
            Severity = Severity.Moderate,
            TreatmentSite = TreatmentSite.Inpatient,
            SpecialInfectionId = Guid.CreateVersion7(),
            OtherCriteriaIds = [Guid.CreateVersion7()],
            MedicineIds = [Guid.CreateVersion7(), Guid.CreateVersion7()],
        };

        var model = _mapper.ToModel(command);

        // The two ID lists are established via navigation (UpdateRelations), not by the mapper
        Assert.Empty(model.MedicineIds);
        Assert.Empty(model.OtherCriteriaIds);
        Assert.Empty(model.Medicines);
        Assert.Empty(model.OtherCriteria);
    }

    # endregion
}
