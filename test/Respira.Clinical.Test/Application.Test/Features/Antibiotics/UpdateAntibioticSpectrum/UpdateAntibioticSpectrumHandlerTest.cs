using Application.Contracts.Data;
using Application.Features.Antibiotics.UpdateAntibioticSpectrum;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.Antibiotics.UpdateAntibioticSpectrum;

public class UpdateAntibioticSpectrumHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly UpdateAntibioticSpectrumHandler _handler;
    private readonly IDbContext _context;

    public UpdateAntibioticSpectrumHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var logger = new Mock<ILogger<UpdateAntibioticSpectrumHandler>>().Object;

        // Initialize handler
        _handler = new(_context, logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        // The antibiotic-pathogen join table is not exposed as a DbSet, so clear it
        // with raw SQL before deleting the entities that reference it.
        // IgnoreQueryFilters is needed because soft-deleted rows are hidden by the
        // query filter but still occupy the table
        var db = (AppDbContext)_context;
        await db.Database.ExecuteSqlRawAsync("DELETE FROM antibiotic_pathogen",
            TestContext.Current.CancellationToken);
        await _context.Dosages.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Antibiotics.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Pathogens.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.AntibioticGroups.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    /*
     * Seeds a group, an antibiotic and three pathogens. initialSpectrumIds are linked
     * to the antibiotic before the update is issued; allIds[0..2] are P1/P2/P3
     */
    private async Task<(Antibiotic Antibiotic, List<Guid> AllPathogenIds)> SeedAsync(
        List<int> initialSpectrumIndexes)
    {
        var group = new AntibioticGroup
        {
            Name = "Beta-lactams",
            Description = "Cell wall synthesis inhibitors sharing the beta-lactam ring",
            ParentId = null,
        };
        var pathogens = new List<Pathogen>
        {
            new() { Name = "Klebsiella pneumoniae", Description = "Gram-negative bacillus" },
            new() { Name = "Pseudomonas aeruginosa", Description = "Gram-negative rod" },
            new() { Name = "Staphylococcus aureus", Description = "Gram-positive coccus" },
        };

        var antibiotic = new Antibiotic
        {
            Name = "Amoxicillin",
            AntibioticGroupId = group.Id,
            Classification = AwareClassification.Access,
        };
        foreach (var index in initialSpectrumIndexes)
            antibiotic.AntibioticSpectra.Add(pathogens[index]);

        await _context.AntibioticGroups.AddAsync(group, TestContext.Current.CancellationToken);
        await _context.Pathogens.AddRangeAsync(pathogens, TestContext.Current.CancellationToken);
        await _context.Antibiotics.AddAsync(antibiotic, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (antibiotic, pathogens.ConvertAll(x => x.Id));
    }

    private async Task<List<Guid>> GetSpectrumIdsAsync(Guid antibioticId)
    {
        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.Antibiotics
            .Include(x => x.AntibioticSpectra)
            .SingleAsync(x => x.Id == antibioticId, TestContext.Current.CancellationToken);
        return [.. saved.AntibioticSpectra.Select(x => x.Id).Order()];
    }

    # region Happy path

    [Fact]
    public async Task UpdateAntibioticSpectrum_AddAndRemoveTogether_Success()
    {
        // Initial [P1] -> command [P2, P3]: P1 must be removed, P2 and P3 added
        var (antibiotic, allIds) = await SeedAsync([0]);

        var result = await _handler.HandleAsync(new UpdateAntibioticSpectrumCommand
        {
            Id = antibiotic.Id,
            PathogenIds = [allIds[1], allIds[2]],
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Updated, result.StatusCode);

        var spectrum = await GetSpectrumIdsAsync(antibiotic.Id);
        Assert.Equal([.. new List<Guid> { allIds[1], allIds[2] }.Order()], spectrum);
    }

    [Fact]
    public async Task UpdateAntibioticSpectrum_AddToExistingSpectrum_Success()
    {
        // Initial [P1] -> command [P1, P2]: P1 kept, P2 added (no duplicate rows)
        var (antibiotic, allIds) = await SeedAsync([0]);

        var result = await _handler.HandleAsync(new UpdateAntibioticSpectrumCommand
        {
            Id = antibiotic.Id,
            PathogenIds = [allIds[0], allIds[1]],
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Updated, result.StatusCode);

        var spectrum = await GetSpectrumIdsAsync(antibiotic.Id);
        Assert.Equal([.. new List<Guid> { allIds[0], allIds[1] }.Order()], spectrum);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task UpdateAntibioticSpectrum_AntibioticNotFound_Fail()
    {
        var unknownId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new UpdateAntibioticSpectrumCommand
            {
                Id = unknownId,
                PathogenIds = [Guid.CreateVersion7()],
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task UpdateAntibioticSpectrum_SomePathogensNotExist_Fail()
    {
        var (antibiotic, allIds) = await SeedAsync([0]);
        var unknownPathogenId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new UpdateAntibioticSpectrumCommand
            {
                Id = antibiotic.Id,
                PathogenIds = [allIds[0], unknownPathogenId],
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // Relations must stay untouched when any pathogen ID does not exist
        var spectrum = await GetSpectrumIdsAsync(antibiotic.Id);
        Assert.Equal([allIds[0]], spectrum);
    }

    [Fact]
    public async Task UpdateAntibioticSpectrum_SoftDeletedPathogen_Fail()
    {
        // A soft-deleted pathogen is hidden by the query filter, so referencing it
        // must be rejected just like an unknown pathogen
        var (antibiotic, _) = await SeedAsync([0]);
        var deletedPathogen = new Pathogen
        {
            Name = "Bordetella pertussis",
            Description = "Whooping cough agent",
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow,
        };
        await _context.Pathogens.AddAsync(deletedPathogen, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _handler.HandleAsync(
            new UpdateAntibioticSpectrumCommand
            {
                Id = antibiotic.Id,
                PathogenIds = [deletedPathogen.Id],
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    # endregion
}
