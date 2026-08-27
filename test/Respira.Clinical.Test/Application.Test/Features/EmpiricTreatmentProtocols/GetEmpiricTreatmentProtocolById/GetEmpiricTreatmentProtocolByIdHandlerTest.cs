using Application.Features.EmpiricTreatmentProtocols.GetEmpiricTreatmentProtocolById;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Exceptions;

namespace Application.Test.Features.EmpiricTreatmentProtocols.GetEmpiricTreatmentProtocolById;

public class GetEmpiricTreatmentProtocolByIdHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly GetEmpiricTreatmentProtocolByIdHandler _handler;
    private readonly AppDbContext _context;

    // Supporting entities seeded once and reused across tests
    private Guid _diseaseId;
    private Guid _pathogenId;
    private Guid _antibioticId;
    private Guid _criterionId;

    public GetEmpiricTreatmentProtocolByIdHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new CriterionResultMapper();

        _handler = new(_context, mapper);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

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
        var criterion = new BooleanCriterion { Name = "Prior history of COPD" };

        _context.Diseases.Add(disease);
        _context.Pathogens.Add(pathogen);
        _context.AntibioticGroups.Add(group);
        _context.Antibiotics.Add(antibiotic);
        _context.Criteria.Add(criterion);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _diseaseId = disease.Id;
        _pathogenId = pathogen.Id;
        _antibioticId = antibiotic.Id;
        _criterionId = criterion.Id;
    }

    private async Task CleanupProtocolsAsync()
    {
        var all = await _context.EmpiricTreatmentProtocols
            .IgnoreQueryFilters()
            .ToListAsync(TestContext.Current.CancellationToken);
        _context.EmpiricTreatmentProtocols.RemoveRange(all);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<EmpiricTreatmentProtocol> SeedProtocolAsync(
        bool withSpecialInfection = true,
        bool withRelations = true,
        bool softDeleted = false)
    {
        var protocol = new EmpiricTreatmentProtocol
        {
            Name = "IDSA/ATS 2024 CAP Empiric Guidance",
            Issuer = "Infectious Diseases Society of America",
            IssueDate = new DateOnly(2024, 8, 1),
            Version = 3,
            DiseaseId = _diseaseId,
            Severity = Severity.Severe,
            TreatmentSite = TreatmentSite.IntensiveCareUnit,
            SpecialInfectionId = withSpecialInfection ? _pathogenId : null,
        };

        await _context.EmpiricTreatmentProtocols.AddAsync(protocol, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        if (withRelations)
        {
            protocol.Medicines = [_context.AttachStub<Antibiotic>(_antibioticId)];
            protocol.OtherCriteria = [_context.AttachStub<Criterion>(_criterionId)];
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        if (softDeleted)
        {
            protocol.IsDeleted = true;
            protocol.DeletedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        return protocol;
    }

    # region Happy path

    [Fact]
    public async Task GetEmpiricTreatmentProtocolById_ReturnsFullGraph_Success()
    {
        await CleanupProtocolsAsync();
        var seeded = await SeedProtocolAsync(withSpecialInfection: true, withRelations: true);

        var result = await _handler.HandleAsync(
            new GetEmpiricTreatmentProtocolByIdQuery { Id = seeded.Id },
            TestContext.Current.CancellationToken);

        Assert.Equal(seeded.Id, result.Id);
        Assert.Equal("IDSA/ATS 2024 CAP Empiric Guidance", result.Name);
        Assert.Equal("Infectious Diseases Society of America", result.Issuer);
        Assert.Equal(new DateOnly(2024, 8, 1), result.IssueDate);
        Assert.Equal(3, result.Version);
        Assert.Equal(Severity.Severe, result.Severity);
        Assert.Equal(TreatmentSite.IntensiveCareUnit, result.TreatmentSite);
        Assert.NotEqual(default, result.UpdatedAt);

        // Business rule: a designated special infection is projected when present
        Assert.NotNull(result.SpecialInfection);
        Assert.Equal(_pathogenId, result.SpecialInfection.Id);
        Assert.Equal("Streptococcus pneumoniae", result.SpecialInfection.Name);

        // Business rule: secondary criteria are mapped through the criterion result mapper
        _ = Assert.Single(result.OtherCriteria);
        var criterion = result.OtherCriteria[0];
        Assert.Equal(_criterionId, criterion.Id);
        Assert.Equal("Prior history of COPD", criterion.Name);
        Assert.Equal(CriterionType.Boolean, criterion.Type);
        Assert.Null(criterion.Value);

        // Business rule: assigned medicines are projected
        _ = Assert.Single(result.Medicines);
        Assert.Equal(_antibioticId, result.Medicines[0].Id);
        Assert.Equal("Amoxicillin", result.Medicines[0].Name);
    }

    [Fact]
    public async Task GetEmpiricTreatmentProtocolById_NoSpecialInfectionOrCriteria_Success()
    {
        await CleanupProtocolsAsync();
        var seeded = await SeedProtocolAsync(withSpecialInfection: false, withRelations: false);

        var result = await _handler.HandleAsync(
            new GetEmpiricTreatmentProtocolByIdQuery { Id = seeded.Id },
            TestContext.Current.CancellationToken);

        Assert.Equal(seeded.Id, result.Id);
        Assert.Equal(3, result.Version);

        // Business rule: when no special infection is assigned, the result is null (not a stub)
        Assert.Null(result.SpecialInfection);
        // Business rule: empty relations yield empty lists, never null
        Assert.Empty(result.OtherCriteria);
        Assert.Empty(result.Medicines);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task GetEmpiricTreatmentProtocolById_NotFound_Fail()
    {
        await CleanupProtocolsAsync();
        var unknownId = Guid.CreateVersion7();

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(
            new GetEmpiricTreatmentProtocolByIdQuery { Id = unknownId },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetEmpiricTreatmentProtocolById_SoftDeletedNotReturned_Fail()
    {
        await CleanupProtocolsAsync();
        var seeded = await SeedProtocolAsync(withSpecialInfection: true, withRelations: true, softDeleted: true);

        // Business rule: soft-deleted protocols are hidden by the query filter and must
        // be reported as not found
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(
            new GetEmpiricTreatmentProtocolByIdQuery { Id = seeded.Id },
            TestContext.Current.CancellationToken));
    }

    # endregion
}
