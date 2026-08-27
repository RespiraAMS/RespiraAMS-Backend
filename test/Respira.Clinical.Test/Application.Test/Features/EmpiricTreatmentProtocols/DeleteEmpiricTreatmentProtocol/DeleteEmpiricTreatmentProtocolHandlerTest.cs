using Application.Features.EmpiricTreatmentProtocols.DeleteEmpiricTreatmentProtocol;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Exceptions;

namespace Application.Test.Features.EmpiricTreatmentProtocols.DeleteEmpiricTreatmentProtocol;

public class DeleteEmpiricTreatmentProtocolHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly DeleteEmpiricTreatmentProtocolHandler _handler;
    private readonly AppDbContext _context;

    // Supporting entities seeded once and reused across tests
    private Guid _diseaseId;
    private Guid _antibioticId;
    private Guid _criterionId;

    public DeleteEmpiricTreatmentProtocolHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var logger = new Mock<ILogger<DeleteEmpiricTreatmentProtocolHandler>>().Object;

        _handler = new(_context, logger);
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
        _context.AntibioticGroups.Add(group);
        _context.Antibiotics.Add(antibiotic);
        _context.Criteria.Add(criterion);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _diseaseId = disease.Id;
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

    private async Task<EmpiricTreatmentProtocol> SeedProtocolAsync(List<Guid>? medicineIds = null,
        List<Guid>? otherCriteriaIds = null)
    {
        var protocol = new EmpiricTreatmentProtocol
        {
            Name = "Legacy 2019 CAP Protocol",
            Issuer = "Old National Guideline Committee",
            IssueDate = new DateOnly(2019, 6, 1),
            Version = 1,
            DiseaseId = _diseaseId,
            Severity = Severity.Mild,
            TreatmentSite = TreatmentSite.Outpatient,
            SpecialInfectionId = null,
        };
        await _context.EmpiricTreatmentProtocols.AddAsync(protocol, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        if (medicineIds is not null)
        {
            protocol.Medicines = medicineIds.ConvertAll(id => _context.AttachStub<Antibiotic>(id));
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        if (otherCriteriaIds is not null)
        {
            protocol.OtherCriteria = otherCriteriaIds.ConvertAll(id => _context.AttachStub<Criterion>(id));
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        return protocol;
    }

    # region Happy path

    [Fact]
    public async Task DeleteEmpiricTreatmentProtocol_WithRelations_SoftDeletesTargetOnly_Success()
    {
        await CleanupProtocolsAsync();
        var target = await SeedProtocolAsync(
            medicineIds: [_antibioticId],
            otherCriteriaIds: [_criterionId]);
        var control = await SeedProtocolAsync();

        await _handler.HandleAsync(new DeleteEmpiricTreatmentProtocolCommand { Id = target.Id },
            TestContext.Current.CancellationToken);

        // Soft-deleted rows are hidden by the query filter, so IgnoreQueryFilters is
        // required to observe the deletion flags
        await using var freshContext = new AppDbContext(_options);

        var deleted = await freshContext.EmpiricTreatmentProtocols.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken);
        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.DeletedAt);

        // The control protocol must stay active
        var untouched = await freshContext.EmpiricTreatmentProtocols.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == control.Id, TestContext.Current.CancellationToken);
        Assert.False(untouched.IsDeleted);
        Assert.Null(untouched.DeletedAt);

        // Business rule: a soft delete removes the protocol from the active (filtered) set
        Assert.False(await freshContext.EmpiricTreatmentProtocols
            .AnyAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken));
        Assert.True(await freshContext.EmpiricTreatmentProtocols
            .AnyAsync(x => x.Id == control.Id, TestContext.Current.CancellationToken));

        // Business rule: linked relations are preserved (only the protocol is soft-deleted)
        Assert.False(await freshContext.Antibiotics.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == _antibioticId && x.IsDeleted, TestContext.Current.CancellationToken));
        Assert.False(await freshContext.Criteria.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == _criterionId && x.IsDeleted, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteEmpiricTreatmentProtocol_NoRelations_Success()
    {
        // Lower boundary of the relation handling: nothing linked to the protocol
        await CleanupProtocolsAsync();
        var target = await SeedProtocolAsync();

        await _handler.HandleAsync(new DeleteEmpiricTreatmentProtocolCommand { Id = target.Id },
            TestContext.Current.CancellationToken);

        await using var freshContext = new AppDbContext(_options);
        var deleted = await freshContext.EmpiricTreatmentProtocols.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken);

        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.DeletedAt);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task DeleteEmpiricTreatmentProtocol_NotFound_Fail()
    {
        await CleanupProtocolsAsync();
        var unknownId = Guid.CreateVersion7();

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(
            new DeleteEmpiricTreatmentProtocolCommand { Id = unknownId },
            TestContext.Current.CancellationToken));

        // Nothing must be soft-deleted when the target does not exist
        Assert.Equal(0, await _context.EmpiricTreatmentProtocols.IgnoreQueryFilters()
            .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken));
    }

    # endregion
}
