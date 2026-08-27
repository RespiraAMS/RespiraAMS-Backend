using Application.Contracts.Mappers;
using Application.Features.EmpiricTreatmentProtocols.UpdateEmpiricTreatmentProtocol;
using Domain.Enums;
using Domain.Models;

namespace Application.Test.Features.EmpiricTreatmentProtocols.UpdateEmpiricTreatmentProtocol;

public class UpdateEmpiricTreatmentProtocolMapperTest
{
    private readonly IUpdateMapper<EmpiricTreatmentProtocol, UpdateEmpiricTreatmentProtocolCommand> _mapper =
        new UpdateEmpiricTreatmentProtocolMapper();

    # region Happy path

    [Fact]
    public void MapModel_WithSpecialInfection_MapsAllScalarFields()
    {
        var diseaseId = Guid.CreateVersion7();
        var specialInfectionId = Guid.CreateVersion7();
        var model = new EmpiricTreatmentProtocol
        {
            Name = "Legacy 2019 CAP Protocol",
            Issuer = "Old National Guideline Committee",
            IssueDate = new DateOnly(2019, 6, 1),
            Version = 1,
            DiseaseId = diseaseId,
            Severity = Severity.Mild,
            TreatmentSite = TreatmentSite.Outpatient,
            SpecialInfectionId = null,
            MedicineIds = [Guid.CreateVersion7()],
            OtherCriteriaIds = [Guid.CreateVersion7()],
        };

        var updatedAtBefore = model.UpdatedAt;

        var command = new UpdateEmpiricTreatmentProtocolCommand
        {
            Id = model.Id,
            Name = "IDSA/ATS 2024 CAP Empiric Guidance",
            Issuer = "Infectious Diseases Society of America",
            IssueDate = new DateOnly(2024, 8, 1),
            Version = 3,
            Severity = Severity.Severe,
            TreatmentSite = TreatmentSite.IntensiveCareUnit,
            SpecialInfectionId = specialInfectionId,
            OtherCriteriaIds = [],
            MedicineIds = [],
        };

        _mapper.MapModel(model, command);

        Assert.Equal(command.Name, model.Name);
        Assert.Equal(command.Issuer, model.Issuer);
        Assert.Equal(command.IssueDate, model.IssueDate);
        Assert.Equal(command.Version, model.Version);
        Assert.Equal(Severity.Severe, model.Severity);
        Assert.Equal(TreatmentSite.IntensiveCareUnit, model.TreatmentSite);
        Assert.Equal(specialInfectionId, model.SpecialInfectionId);

        // Update timestamp must be refreshed on every mapping
        Assert.NotEqual(updatedAtBefore, model.UpdatedAt);

        // Disease linkage is never changed by an update command
        Assert.Equal(diseaseId, model.DiseaseId);
    }

    [Fact]
    public void MapModel_WithoutSpecialInfection_KeepsNullSpecialInfectionId()
    {
        var model = new EmpiricTreatmentProtocol
        {
            Name = "Legacy 2019 CAP Protocol",
            Issuer = "Old National Guideline Committee",
            IssueDate = new DateOnly(2019, 6, 1),
            Version = 1,
            DiseaseId = Guid.CreateVersion7(),
            Severity = Severity.Mild,
            TreatmentSite = TreatmentSite.Outpatient,
            SpecialInfectionId = Guid.CreateVersion7(),
        };

        var command = new UpdateEmpiricTreatmentProtocolCommand
        {
            Id = model.Id,
            Name = "Vietnam MOH 2023 Basic CAP Protocol",
            Issuer = "Vietnam Ministry of Health",
            IssueDate = new DateOnly(2023, 5, 15),
            Version = 2,
            Severity = Severity.Moderate,
            TreatmentSite = TreatmentSite.Inpatient,
            SpecialInfectionId = null,
            OtherCriteriaIds = [],
            MedicineIds = [Guid.CreateVersion7()],
        };

        _mapper.MapModel(model, command);

        Assert.Equal(command.Name, model.Name);
        Assert.Equal(command.Issuer, model.Issuer);
        Assert.Equal(command.IssueDate, model.IssueDate);
        Assert.Equal(command.Version, model.Version);
        Assert.Equal(Severity.Moderate, model.Severity);
        Assert.Equal(TreatmentSite.Inpatient, model.TreatmentSite);
        Assert.Null(model.SpecialInfectionId);
    }

    [Fact]
    public void MapModel_IgnoresRelationIdLists()
    {
        var preExistingMedicines = new List<Guid> { Guid.CreateVersion7() };
        var preExistingCriteria = new List<Guid> { Guid.CreateVersion7() };
        var model = new EmpiricTreatmentProtocol
        {
            Name = "Legacy 2019 CAP Protocol",
            Issuer = "Old National Guideline Committee",
            IssueDate = new DateOnly(2019, 6, 1),
            Version = 1,
            DiseaseId = Guid.CreateVersion7(),
            Severity = Severity.Mild,
            TreatmentSite = TreatmentSite.Outpatient,
            SpecialInfectionId = null,
            MedicineIds = preExistingMedicines,
            OtherCriteriaIds = preExistingCriteria,
        };

        var command = new UpdateEmpiricTreatmentProtocolCommand
        {
            Id = model.Id,
            Name = "IDSA/ATS 2024 CAP Empiric Guidance",
            Issuer = "Infectious Diseases Society of America",
            IssueDate = new DateOnly(2024, 8, 1),
            Version = 3,
            Severity = Severity.Severe,
            TreatmentSite = TreatmentSite.IntensiveCareUnit,
            SpecialInfectionId = Guid.CreateVersion7(),
            OtherCriteriaIds = [Guid.CreateVersion7()],
            MedicineIds = [Guid.CreateVersion7(), Guid.CreateVersion7()],
        };

        _mapper.MapModel(model, command);

        // The two ID lists are established via navigation (UpdateRelations), not by the mapper,
        // so they must be left untouched by the mapping.
        Assert.Equal(preExistingMedicines, model.MedicineIds);
        Assert.Equal(preExistingCriteria, model.OtherCriteriaIds);
    }

    # endregion
}
