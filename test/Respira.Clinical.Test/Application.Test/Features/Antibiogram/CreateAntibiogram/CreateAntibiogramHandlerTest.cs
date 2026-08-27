using Application.Contracts.Data;
using Application.Features.Antibiograms.CreateAntibiogram;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Exceptions;

namespace Application.Test.Features.Antibiogram.CreateAntibiogram;

public class CreateAntibiogramHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly CreateAntibiogramHandler _handler;
    private readonly IDbContext _context;

    public CreateAntibiogramHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new CreateAntibiogramMapper();
        var logger = new Mock<ILogger<CreateAntibiogramHandler>>().Object;

        // Initialize handler
        _handler = new(_context, mapper, logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        // The antibiogram-antibiotic join tables are not exposed as DbSets, so clear
        // them with raw SQL before deleting the entities that reference them.
        // IgnoreQueryFilters is needed because soft-deleted rows are hidden by the
        // query filter but still occupy the table
        var db = (AppDbContext)_context;
        await db.Database.ExecuteSqlRawAsync("DELETE FROM antibiogram_mic_groups",
            TestContext.Current.CancellationToken);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM antibiogram_first_priority_medicines",
            TestContext.Current.CancellationToken);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM antibiogram_second_priority_medicines",
            TestContext.Current.CancellationToken);
        await _context.Antibiograms.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
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
     * Seeds one group, one pathogen and antibioticCount antibiotics. Pathogen and
     * antibiotics can be seeded soft-deleted to exercise the query filters
     */
    private async Task<(Guid PathogenId, List<Guid> AntibioticIds)> SeedAsync(
        int antibioticCount, bool softDeletedPathogen = false, bool softDeletedFirstAntibiotic = false)
    {
        var group = new AntibioticGroup
        {
            Name = "Beta-lactams",
            Description = "Cell wall synthesis inhibitors sharing the beta-lactam ring",
            ParentId = null,
        };
        await _context.AntibioticGroups.AddAsync(group, TestContext.Current.CancellationToken);

        var pathogen = new Pathogen
        {
            Name = "Klebsiella pneumoniae",
            Description = "Gram-negative bacillus",
            IsDeleted = softDeletedPathogen,
            DeletedAt = softDeletedPathogen ? DateTimeOffset.UtcNow : null,
        };
        await _context.Pathogens.AddAsync(pathogen, TestContext.Current.CancellationToken);

        var names = new[] { "Amoxicillin", "Meropenem", "Ciprofloxacin", "Gentamicin", "Doxycycline" };
        var antibiotics = new List<Antibiotic>();
        for (var i = 0; i < antibioticCount; i++)
        {
            antibiotics.Add(new Antibiotic
            {
                Name = names[i % names.Length] + (i >= names.Length ? $" #{i}" : ""),
                AntibioticGroupId = group.Id,
                Classification = AwareClassification.Access,
                IsDeleted = i == 0 && softDeletedFirstAntibiotic,
                DeletedAt = i == 0 && softDeletedFirstAntibiotic ? DateTimeOffset.UtcNow : null,
            });
        }
        await _context.Antibiotics.AddRangeAsync(antibiotics, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (pathogen.Id, antibiotics.ConvertAll(x => x.Id));
    }

    # region Happy path

    [Fact]
    public async Task CreateAntibiogram_WithAllRelations_Success()
    {
        /*
         * Boundary: a single-element MIC list is the lower bound of the non-empty
         * rule while first and second priority each carry their own antibiotic.
         * CLSI breakpoint: Resistance level for Klebsiella against third-generation
         * cephalosporins
         */
        var (pathogenId, antibioticIds) = await SeedAsync(antibioticCount: 3);

        var result = await _handler.HandleAsync(new CreateAntibiogramCommand
        {
            PathogenId = pathogenId,
            MicLevel = MinimumInhibitoryConcentration.Resistance,
            MicIds = [antibioticIds[0]],
            FirstPriorityMedicineIds = [antibioticIds[1]],
            SecondPriorityMedicineIds = [antibioticIds[2]],
        }, TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, result.Id);

        // Verify through a fresh context so the change tracker cannot mask a failed commit
        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.Antibiograms
            .Include(x => x.Mics)
            .Include(x => x.FirstPriorityMedicines)
            .Include(x => x.SecondPriorityMedicines)
            .SingleAsync(x => x.Id == result.Id, TestContext.Current.CancellationToken);

        Assert.Equal(pathogenId, saved.PathogenId);
        Assert.Equal(MinimumInhibitoryConcentration.Resistance, saved.MicLevel);
        Assert.Equal([antibioticIds[0]], saved.Mics.ConvertAll(x => x.Id));
        Assert.Equal([antibioticIds[1]], saved.FirstPriorityMedicines.ConvertAll(x => x.Id));
        Assert.Equal([antibioticIds[2]], saved.SecondPriorityMedicines.ConvertAll(x => x.Id));
    }

    [Fact]
    public async Task CreateAntibiogram_MultipleMicsPerList_Success()
    {
        var (pathogenId, antibioticIds) = await SeedAsync(antibioticCount: 5);

        var result = await _handler.HandleAsync(new CreateAntibiogramCommand
        {
            PathogenId = pathogenId,
            MicLevel = MinimumInhibitoryConcentration.Intermediate,
            MicIds = [antibioticIds[0], antibioticIds[1]],
            FirstPriorityMedicineIds = [antibioticIds[2], antibioticIds[3]],
            SecondPriorityMedicineIds = [antibioticIds[4]],
        }, TestContext.Current.CancellationToken);

        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.Antibiograms
            .Include(x => x.Mics)
            .Include(x => x.FirstPriorityMedicines)
            .Include(x => x.SecondPriorityMedicines)
            .SingleAsync(x => x.Id == result.Id, TestContext.Current.CancellationToken);

        Assert.Equal(2, saved.Mics.Count);
        Assert.Equal(2, saved.FirstPriorityMedicines.Count);
        _ = Assert.Single(saved.SecondPriorityMedicines);
    }

    [Fact]
    public async Task CreateAntibiogram_DuplicateAntibioticAcrossLists_Fail()
    {
        var (pathogenId, antibioticIds) = await SeedAsync(antibioticCount: 3);

        // The same antibiotic appears as MIC and as first priority
        await _handler.HandleAsync(
            new CreateAntibiogramCommand
            {
                PathogenId = pathogenId,
                MicLevel = MinimumInhibitoryConcentration.Resistance,
                MicIds = [antibioticIds[0], antibioticIds[1]],
                FirstPriorityMedicineIds = [antibioticIds[0]],
                SecondPriorityMedicineIds = [antibioticIds[2]],
            }, TestContext.Current.CancellationToken);

        Assert.Equal(1, await _context.Antibiograms.CountAsync(TestContext.Current.CancellationToken));
    }


    # endregion

    # region Fail path

    [Fact]
    public async Task CreateAntibiogram_PathogenNotFound_Fail()
    {
        var unknownId = Guid.CreateVersion7();

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(
            new CreateAntibiogramCommand
            {
                PathogenId = unknownId,
                MicLevel = MinimumInhibitoryConcentration.Susceptible,
                MicIds = [Guid.CreateVersion7()],
                FirstPriorityMedicineIds = [Guid.CreateVersion7()],
                SecondPriorityMedicineIds = [Guid.CreateVersion7()],
            }, TestContext.Current.CancellationToken));

        Assert.Equal(0, await _context.Antibiograms.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAntibiogram_SoftDeletedPathogen_Fail()
    {
        // A soft-deleted pathogen is hidden by the query filter, so referencing it
        // must be rejected just like an unknown pathogen
        var (deletedPathogenId, _) = await SeedAsync(antibioticCount: 1, softDeletedPathogen: true);

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(
            new CreateAntibiogramCommand
            {
                PathogenId = deletedPathogenId,
                MicLevel = MinimumInhibitoryConcentration.Susceptible,
                MicIds = [Guid.CreateVersion7()],
                FirstPriorityMedicineIds = [Guid.CreateVersion7()],
                SecondPriorityMedicineIds = [Guid.CreateVersion7()],
            }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAntibiogram_SomeAntibioticsNotExist_Fail()
    {
        var (pathogenId, antibioticIds) = await SeedAsync(antibioticCount: 3);
        var unknownId = Guid.CreateVersion7();

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(
            new CreateAntibiogramCommand
            {
                PathogenId = pathogenId,
                MicLevel = MinimumInhibitoryConcentration.Resistance,
                MicIds = [antibioticIds[0], unknownId],
                FirstPriorityMedicineIds = [antibioticIds[1]],
                SecondPriorityMedicineIds = [antibioticIds[2]],
            }, TestContext.Current.CancellationToken));

        // Nothing must be created when any referenced antibiotic does not exist
        Assert.Equal(0, await _context.Antibiograms.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAntibiogram_SoftDeletedAntibioticAmongIds_Fail()
    {
        // A soft-deleted antibiotic is hidden by the query filter, so referencing it
        // must be rejected just like an unknown antibiotic
        var (pathogenId, antibioticIds) = await SeedAsync(
            antibioticCount: 3, softDeletedFirstAntibiotic: true);

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(
            new CreateAntibiogramCommand
            {
                PathogenId = pathogenId,
                MicLevel = MinimumInhibitoryConcentration.Intermediate,
                MicIds = [antibioticIds[0]],
                FirstPriorityMedicineIds = [antibioticIds[1]],
                SecondPriorityMedicineIds = [antibioticIds[2]],
            }, TestContext.Current.CancellationToken));

        Assert.Equal(0, await _context.Antibiograms.CountAsync(TestContext.Current.CancellationToken));
    }

    # endregion
}
