using Application.Contracts.Data;
using Application.Features.Pathogens.DeletePathogen;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.Pathogens.DeletePathogen;

public class DeletePathogenHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly DeletePathogenHandler _handler;
    private readonly IDbContext _context;

    public DeletePathogenHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var logger = new Mock<ILogger<DeletePathogenHandler>>().Object;

        // Initialize handler
        _handler = new(_context, logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        // Clear leftover data (children first for FK constraints) so lookups stay
        // deterministic across runs. Soft-deleted rows are removed as well since the
        // query filters would otherwise hide them from ExecuteDeleteAsync
        await _context.EmpiricTreatmentProtocols.IgnoreQueryFilters().ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.ResistanceRiskFactors.IgnoreQueryFilters().ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Causes.IgnoreQueryFilters().ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Pathogens.IgnoreQueryFilters().ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Diseases.IgnoreQueryFilters().ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Criteria.IgnoreQueryFilters().ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    /*
     * Seeds a disease, a target pathogen and an unrelated control pathogen. When
     * withRelatedData is set, the target also gets one cause, one resistance risk
     * factor and one treatment protocol pointing at it via SpecialInfectionId,
     * while the control pathogen gets its own cause and risk factor
     */
    private async Task<Pathogen> SeedPathogenAsync(bool withRelatedData)
    {
        var disease = new Disease
        {
            Name = "Hospital-acquired pneumonia",
            Description = "Pneumonia occurring 48 hours or more after hospital admission",
            IcuScoreThreshold = 5,
        };
        var criterion = new BooleanCriterion { Name = "Carbapenem exposure within the last 90 days" };
        var target = new Pathogen
        {
            Name = "Klebsiella pneumoniae",
            Description = "Gram-negative bacillus causing hospital-acquired pneumonia",
        };
        var other = new Pathogen
        {
            Name = "Pseudomonas aeruginosa",
            Description = "Gram-negative rod, common cause of ventilator-associated pneumonia",
        };

        await _context.Diseases.AddAsync(disease, TestContext.Current.CancellationToken);
        await _context.Pathogens.AddRangeAsync([target, other], TestContext.Current.CancellationToken);
        await _context.Criteria.AddAsync(criterion, TestContext.Current.CancellationToken);

        // Save principals first so the children below can safely reference them
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        if (withRelatedData)
        {
            await _context.Causes.AddRangeAsync([
                new Cause
                {
                    DiseaseId = disease.Id, PathogenId = target.Id,
                    Severity = Severity.Moderate, TreatmentSite = TreatmentSite.Inpatient,
                },
                new Cause
                {
                    DiseaseId = disease.Id, PathogenId = other.Id,
                    Severity = Severity.Severe, TreatmentSite = TreatmentSite.IntensiveCareUnit,
                }], TestContext.Current.CancellationToken);

            await _context.ResistanceRiskFactors.AddRangeAsync([
                new ResistanceRiskFactor
                {
                    DiseaseId = disease.Id, PathogenId = target.Id,
                    CriterionId = criterion.Id,
                    Name = "Carbapenem exposure within the last 90 days",
                },
                new ResistanceRiskFactor
                {
                    DiseaseId = disease.Id, PathogenId = other.Id,
                    CriterionId = criterion.Id,
                    Name = "Neutropenia (ANC below 500/mm3)",
                }], TestContext.Current.CancellationToken);

            await _context.EmpiricTreatmentProtocols.AddRangeAsync([
                new EmpiricTreatmentProtocol
                {
                    Name = "HAP empiric therapy targeting Klebsiella",
                    Issuer = "VietNam Ministry of Health",
                    IssueDate = new DateOnly(2025, 3, 1),
                    Version = 1,
                    DiseaseId = disease.Id,
                    Severity = Severity.Moderate,
                    TreatmentSite = TreatmentSite.Inpatient,
                    SpecialInfectionId = target.Id,
                },
                new EmpiricTreatmentProtocol
                {
                    Name = "General HAP first-line guideline",
                    Issuer = "WHO",
                    IssueDate = new DateOnly(2024, 11, 20),
                    Version = 2,
                    DiseaseId = disease.Id,
                    Severity = Severity.Severe,
                    TreatmentSite = TreatmentSite.IntensiveCareUnit,
                }], TestContext.Current.CancellationToken);
        }

        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return target;
    }

    # region Happy path

    [Fact]
    public async Task DeletePathogen_WithRelatedRows_CascadesSoftDelete_Success()
    {
        var seeded = await SeedPathogenAsync(withRelatedData: true);

        var result = await _handler.HandleAsync(new DeletePathogenCommand(seeded.Id), TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Deleted, result.StatusCode);

        // All entities carry a !IsDeleted query filter, so IgnoreQueryFilters is
        // required to observe the soft-delete flags
        await using var freshContext = new AppDbContext(_options);

        var deletedPathogen = await freshContext.Pathogens.IgnoreQueryFilters()
            .SingleAsync(p => p.Id == seeded.Id, TestContext.Current.CancellationToken);
        Assert.True(deletedPathogen.IsDeleted);
        Assert.NotNull(deletedPathogen.DeletedAt);

        // Cascade: cause, resistance risk factor and protocol tied to the deleted pathogen
        var deletedCause = await freshContext.Causes.IgnoreQueryFilters()
            .SingleAsync(x => x.PathogenId == seeded.Id, TestContext.Current.CancellationToken);
        Assert.True(deletedCause.IsDeleted);
        Assert.NotNull(deletedCause.DeletedAt);

        var deletedFactor = await freshContext.ResistanceRiskFactors.IgnoreQueryFilters()
            .SingleAsync(x => x.PathogenId == seeded.Id, TestContext.Current.CancellationToken);
        Assert.True(deletedFactor.IsDeleted);
        Assert.NotNull(deletedFactor.DeletedAt);

        var deletedProtocol = await freshContext.EmpiricTreatmentProtocols.IgnoreQueryFilters()
            .SingleAsync(x => x.SpecialInfectionId == seeded.Id, TestContext.Current.CancellationToken);
        Assert.True(deletedProtocol.IsDeleted);
        Assert.NotNull(deletedProtocol.DeletedAt);

        // Rows of other pathogens must stay untouched
        Assert.False(await freshContext.Pathogens.IgnoreQueryFilters()
            .AnyAsync(p => p.Name.StartsWith("Pseudomonas") && p.IsDeleted, TestContext.Current.CancellationToken));
        Assert.False(await freshContext.Causes.IgnoreQueryFilters()
            .AnyAsync(x => x.PathogenId != seeded.Id && x.IsDeleted, TestContext.Current.CancellationToken));
        Assert.False(await freshContext.ResistanceRiskFactors.IgnoreQueryFilters()
            .AnyAsync(x => x.PathogenId != seeded.Id && x.IsDeleted, TestContext.Current.CancellationToken));
        Assert.Equal(1, await freshContext.EmpiricTreatmentProtocols.IgnoreQueryFilters()
            .CountAsync(x => !x.IsDeleted, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeletePathogen_NoRelatedRows_Success()
    {
        // Lower boundary of the cascade: nothing linked to the pathogen
        var seeded = await SeedPathogenAsync(withRelatedData: false);

        var result = await _handler.HandleAsync(new DeletePathogenCommand(seeded.Id), TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Deleted, result.StatusCode);

        await using var freshContext = new AppDbContext(_options);
        var deleted = await freshContext.Pathogens.IgnoreQueryFilters()
            .SingleAsync(p => p.Id == seeded.Id, TestContext.Current.CancellationToken);

        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.DeletedAt);
        Assert.True(await freshContext.Pathogens.IgnoreQueryFilters()
            .AnyAsync(p => !p.IsDeleted, TestContext.Current.CancellationToken));
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task DeletePathogen_PathogenNotFound_Fail()
    {
        var unknownId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(new DeletePathogenCommand(unknownId), TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // Nothing must be soft-deleted when the target does not exist
        Assert.Equal(0, await _context.Pathogens.IgnoreQueryFilters()
            .CountAsync(TestContext.Current.CancellationToken));
    }

    # endregion
}
