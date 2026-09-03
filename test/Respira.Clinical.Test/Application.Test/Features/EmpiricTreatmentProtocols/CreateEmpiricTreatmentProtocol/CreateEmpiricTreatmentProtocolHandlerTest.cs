using Application.Features.EmpiricTreatmentProtocols.CreateEmpiricTreatmentProtocol;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.EmpiricTreatmentProtocols.CreateEmpiricTreatmentProtocol;

public class CreateEmpiricTreatmentProtocolHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly CreateEmpiricTreatmentProtocolHandler _handler;
    private readonly AppDbContext _context;

    public CreateEmpiricTreatmentProtocolHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new CreateEmpiricTreatmentProtocolMapper();
        var logger = new Mock<ILogger<CreateEmpiricTreatmentProtocolHandler>>().Object;

        _handler = new(_context, mapper, logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    // Supporting entities seeded once and reused across tests
    private Guid _diseaseId;
    private Guid _pathogenId;
    private Guid _criterionId;
    private Guid _antibioticId;
    private Guid _antibioticId2;

    public async ValueTask InitializeAsync()
    {
        await CleanupProtocolsAsync();

        var disease = new Disease
        {
            Name = "Community-Acquired Pneumonia",
            Description = "Infection of the lung parenchyma acquired outside of a healthcare setting",
            IcuScoreThreshold = 3,
        };
        var pathogen = new Pathogen
        {
            Name = "Streptococcus pneumoniae",
            Description = "Gram-positive coccus and the most common bacterial cause of CAP",
        };
        var criterion = new BooleanCriterion { Name = "Prior history of COPD" };
        var group = new AntibioticGroup
        {
            Name = "Beta-lactams",
            Description = "Antibiotics that share the beta-lactam ring core structure",
            ParentId = null,
        };
        var antibiotic = new Antibiotic
        {
            Name = "Amoxicillin",
            AntibioticGroupId = group.Id,
            Classification = AwareClassification.Access,
        };
        var antibiotic2 = new Antibiotic
        {
            Name = "Ceftriaxone",
            AntibioticGroupId = group.Id,
            Classification = AwareClassification.Watch,
        };

        _context.Diseases.Add(disease);
        _context.Pathogens.Add(pathogen);
        _context.Criteria.Add(criterion);
        _context.AntibioticGroups.Add(group);
        _context.Antibiotics.Add(antibiotic);
        _context.Antibiotics.Add(antibiotic2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _diseaseId = disease.Id;
        _pathogenId = pathogen.Id;
        _criterionId = criterion.Id;
        _antibioticId = antibiotic.Id;
        _antibioticId2 = antibiotic2.Id;
    }

    private async Task CleanupProtocolsAsync()
    {
        var all = await _context.EmpiricTreatmentProtocols
            .IgnoreQueryFilters()
            .ToListAsync(TestContext.Current.CancellationToken);
        _context.EmpiricTreatmentProtocols.RemoveRange(all);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    # region Happy path

    [Fact]
    public async Task CreateEmpiricTreatmentProtocol_WithAllRelations_Success()
    {
        await CleanupProtocolsAsync();

        var command = new CreateEmpiricTreatmentProtocolCommand
        {
            Name = "IDSA/ATS 2024 CAP Empiric Guidance",
            Issuer = "Infectious Diseases Society of America",
            IssueDate = new DateOnly(2024, 8, 1),
            Version = 1,
            DiseaseId = _diseaseId,
            Severity = Severity.Moderate,
            TreatmentSite = TreatmentSite.Inpatient,
            SpecialInfectionId = _pathogenId,
            OtherCriteriaIds = [_criterionId],
            MedicineIds = [_antibioticId, _antibioticId2],
        };

        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Created, result.StatusCode);
        Assert.NotNull(result.Data);

        Assert.NotEqual(Guid.Empty, result.Data.Id);

        // Verify through a fresh context so the change tracker cannot mask a failed commit
        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.EmpiricTreatmentProtocols
            .Include(p => p.OtherCriteria)
            .Include(p => p.Medicines)
            .Include(p => p.SpecialInfection)
            .SingleAsync(p => p.Id == result.Data.Id, TestContext.Current.CancellationToken);

        Assert.Equal(command.Name, saved.Name);
        Assert.Equal(command.Issuer, saved.Issuer);
        Assert.Equal(command.IssueDate, saved.IssueDate);
        Assert.Equal(command.Version, saved.Version);
        Assert.Equal(_diseaseId, saved.DiseaseId);
        Assert.Equal(Severity.Moderate, saved.Severity);
        Assert.Equal(TreatmentSite.Inpatient, saved.TreatmentSite);

        // Business rule: a designated special infection must be persisted
        Assert.Equal(_pathogenId, saved.SpecialInfectionId);
        Assert.NotNull(saved.SpecialInfection);
        Assert.Equal(_pathogenId, saved.SpecialInfection.Id);

        // Business rule: all supplied secondary criteria must be linked
        Assert.Single(saved.OtherCriteria);
        Assert.Contains(saved.OtherCriteria, c => c.Id == _criterionId);

        // Business rule: all supplied medicines must be linked
        Assert.Collection(saved.Medicines, _ => { }, _ => { });
        Assert.Contains(saved.Medicines, m => m.Id == _antibioticId);
        Assert.Contains(saved.Medicines, m => m.Id == _antibioticId2);
    }

    [Fact]
    public async Task CreateEmpiricTreatmentProtocol_WithoutSpecialInfectionOrCriteria_Success()
    {
        await CleanupProtocolsAsync();

        var command = new CreateEmpiricTreatmentProtocolCommand
        {
            Name = "Vietnam MOH 2023 Basic CAP Protocol",
            Issuer = "Vietnam Ministry of Health",
            IssueDate = new DateOnly(2023, 5, 15),
            Version = 1,
            DiseaseId = _diseaseId,
            Severity = Severity.Mild,
            TreatmentSite = TreatmentSite.Outpatient,
            SpecialInfectionId = null,
            OtherCriteriaIds = [],
            MedicineIds = [_antibioticId],
        };

        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Created, result.StatusCode);
        Assert.NotNull(result.Data);

        Assert.NotEqual(Guid.Empty, result.Data.Id);

        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.EmpiricTreatmentProtocols
            .Include(p => p.OtherCriteria)
            .Include(p => p.Medicines)
            .Include(p => p.SpecialInfection)
            .SingleAsync(p => p.Id == result.Data.Id, TestContext.Current.CancellationToken);

        Assert.Equal(_diseaseId, saved.DiseaseId);
        Assert.Null(saved.SpecialInfectionId);
        Assert.Null(saved.SpecialInfection);
        Assert.Empty(saved.OtherCriteria);
        _ = Assert.Single(saved.Medicines);
        Assert.Equal(_antibioticId, saved.Medicines[0].Id);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task CreateEmpiricTreatmentProtocol_UnknownDisease_Fail()
    {
        await CleanupProtocolsAsync();

        var unknownDiseaseId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new CreateEmpiricTreatmentProtocolCommand
            {
                Name = "WHO 2024 CAP Guidance",
                Issuer = "World Health Organization",
                IssueDate = new DateOnly(2024, 1, 10),
                Version = 1,
                DiseaseId = unknownDiseaseId,
                Severity = Severity.Severe,
                TreatmentSite = TreatmentSite.IntensiveCareUnit,
                SpecialInfectionId = null,
                OtherCriteriaIds = [],
                MedicineIds = [_antibioticId],
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // Nothing must be created when the disease does not exist
        Assert.Empty(await _context.EmpiricTreatmentProtocols
            .ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateEmpiricTreatmentProtocol_UnknownSpecialInfection_Fail()
    {
        await CleanupProtocolsAsync();

        var unknownPathogenId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new CreateEmpiricTreatmentProtocolCommand
            {
                Name = "WHO 2024 CAP Guidance",
                Issuer = "World Health Organization",
                IssueDate = new DateOnly(2024, 1, 10),
                Version = 1,
                DiseaseId = _diseaseId,
                Severity = Severity.Severe,
                TreatmentSite = TreatmentSite.IntensiveCareUnit,
                SpecialInfectionId = unknownPathogenId,
                OtherCriteriaIds = [],
                MedicineIds = [_antibioticId],
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        Assert.Empty(await _context.EmpiricTreatmentProtocols
            .ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateEmpiricTreatmentProtocol_NotAllCriteriaExist_Fail()
    {
        await CleanupProtocolsAsync();

        var unknownCriterionId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new CreateEmpiricTreatmentProtocolCommand
            {
                Name = "WHO 2024 CAP Guidance",
                Issuer = "World Health Organization",
                IssueDate = new DateOnly(2024, 1, 10),
                Version = 1,
                DiseaseId = _diseaseId,
                Severity = Severity.Moderate,
                TreatmentSite = TreatmentSite.Inpatient,
                SpecialInfectionId = null,
                OtherCriteriaIds = [_criterionId, unknownCriterionId],
                MedicineIds = [_antibioticId],
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        Assert.Empty(await _context.EmpiricTreatmentProtocols
            .ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateEmpiricTreatmentProtocol_NotAllMedicinesExist_Fail()
    {
        await CleanupProtocolsAsync();

        var unknownMedicineId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new CreateEmpiricTreatmentProtocolCommand
            {
                Name = "WHO 2024 CAP Guidance",
                Issuer = "World Health Organization",
                IssueDate = new DateOnly(2024, 1, 10),
                Version = 1,
                DiseaseId = _diseaseId,
                Severity = Severity.Moderate,
                TreatmentSite = TreatmentSite.Inpatient,
                SpecialInfectionId = null,
                OtherCriteriaIds = [_criterionId],
                MedicineIds = [_antibioticId, unknownMedicineId],
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        Assert.Empty(await _context.EmpiricTreatmentProtocols
            .ToListAsync(TestContext.Current.CancellationToken));
    }

    # endregion
}
