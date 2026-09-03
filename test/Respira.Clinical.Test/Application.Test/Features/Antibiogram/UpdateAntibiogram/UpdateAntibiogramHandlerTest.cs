using Application.Contracts.Data;
using Application.Features.Antibiograms.UpdateAntibiogram;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.Antibiogram.UpdateAntibiogram;

using Antibiogram = Domain.Models.Antibiogram;

public class UpdateAntibiogramHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly UpdateAntibiogramHandler _handler;
    private readonly IDbContext _context;

    public UpdateAntibiogramHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new UpdateAntibiogramMapper();
        var logger = new Mock<ILogger<UpdateAntibiogramHandler>>().Object;

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
     * Seeds one group, one pathogen and antibioticCount antibiotics. When
     * seedAntibiogram is set, an existing antibiogram is created with mic = [A0],
     * first priority = [A1], second priority = [A2] and optionally a control
     * antibiogram sharing the same antibiotics, so relation rewrites can be told
     * apart from untouched rows. Flags allow soft-deleting the first antibiotic or
     * the antibiogram itself to exercise the query filters
     */
    private async Task<(Guid PathogenId, List<Guid> AntibioticIds, Antibiogram? Antibiogram)> SeedAsync(
        int antibioticCount,
        bool seedAntibiogram = true,
        bool seedControlAntibiogram = false,
        bool softDeletedFirstAntibiotic = false,
        bool softDeletedAntibiogram = false)
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

        Antibiogram? antibiogram = null;
        if (seedAntibiogram)
        {
            antibiogram = new Antibiogram
            {
                PathogenId = pathogen.Id,
                MicLevel = MinimumInhibitoryConcentration.Susceptible,
                Mics = [antibiotics[0]],
                FirstPriorityMedicines = [antibiotics[1]],
                SecondPriorityMedicines = [antibiotics[2]],
                IsDeleted = softDeletedAntibiogram,
                DeletedAt = softDeletedAntibiogram ? DateTimeOffset.UtcNow : null,
            };
            await _context.Antibiograms.AddAsync(antibiogram, TestContext.Current.CancellationToken);

            if (seedControlAntibiogram)
            {
                var control = new Antibiogram
                {
                    PathogenId = pathogen.Id,
                    MicLevel = MinimumInhibitoryConcentration.Intermediate,
                    Mics = [antibiotics[0]],
                    FirstPriorityMedicines = [antibiotics[1]],
                    SecondPriorityMedicines = [antibiotics[2]],
                };
                await _context.Antibiograms.AddAsync(control, TestContext.Current.CancellationToken);
            }
        }

        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (pathogen.Id, antibiotics.ConvertAll(x => x.Id), antibiogram);
    }

    private async Task<Antibiogram> GetPersistedAntibiogramAsync(Guid id)
    {
        await using var freshContext = new AppDbContext(_options);
        return await freshContext.Antibiograms
            .Include(x => x.Mics)
            .Include(x => x.FirstPriorityMedicines)
            .Include(x => x.SecondPriorityMedicines)
            .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
    }

    # region Happy path

    [Fact]
    public async Task UpdateAntibiogram_MicLevelAndRelations_Success()
    {
        // Rewrite all three lists: mic [A0] -> [A3, A4], first [A1] -> [A1] kept,
        // second [A2] -> [A4]; CLSI breakpoint flips Susceptible -> Resistance
        var (_, antibioticIds, antibiogram) = await SeedAsync(antibioticCount: 5, seedControlAntibiogram: true);
        _ = antibiogram ?? throw new InvalidOperationException("Seed failure");

        var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
        {
            Id = antibiogram.Id,
            MicLevel = MinimumInhibitoryConcentration.Resistance,
            MicIds = [antibioticIds[3], antibioticIds[4]],
            FirstPriorityMedicineIds = [antibioticIds[1]],
            SecondPriorityMedicineIds = [antibioticIds[4]],
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Updated, result.StatusCode);

        // Verify through a fresh context so the change tracker cannot mask a failed commit
        var saved = await GetPersistedAntibiogramAsync(antibiogram.Id);
        Assert.Equal(MinimumInhibitoryConcentration.Resistance, saved.MicLevel);
        Assert.Equal([.. new List<Guid> { antibioticIds[3], antibioticIds[4] }.Order()],
            saved.Mics.Select(x => x.Id).Order().ToList());
        Assert.Equal([antibioticIds[1]], saved.FirstPriorityMedicines.ConvertAll(x => x.Id));
        Assert.Equal([antibioticIds[4]], saved.SecondPriorityMedicines.ConvertAll(x => x.Id));

        // The pathogen link is immutable through updates
        Assert.True(await _context.Antibiograms.AnyAsync(
            x => x.Id == antibiogram.Id && x.PathogenId == saved.PathogenId,
            TestContext.Current.CancellationToken));

        // The control antibiogram must keep its own relations untouched
        await using var freshContext = new AppDbContext(_options);
        var control = await freshContext.Antibiograms
            .Include(x => x.Mics)
            .SingleAsync(x => x.MicLevel == MinimumInhibitoryConcentration.Intermediate,
                TestContext.Current.CancellationToken);
        Assert.Equivalent(new List<Guid> { antibioticIds[0] }, control.Mics.ConvertAll(x => x.Id));
    }

    [Fact]
    public async Task UpdateAntibiogram_AddAndRemoveTogether_Success()
    {
        // mic [A0] -> command [A1, A2]: A0 removed, A1 and A2 added in one update
        var (_, antibioticIds, antibiogram) = await SeedAsync(antibioticCount: 3);
        _ = antibiogram ?? throw new InvalidOperationException("Seed failure");

        var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
        {
            Id = antibiogram.Id,
            MicLevel = MinimumInhibitoryConcentration.Intermediate,
            MicIds = [antibioticIds[1], antibioticIds[2]],
            FirstPriorityMedicineIds = [antibioticIds[1]],
            SecondPriorityMedicineIds = [antibioticIds[2]],
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Updated, result.StatusCode);

        var saved = await GetPersistedAntibiogramAsync(antibiogram.Id);
        Assert.Equal(2, saved.Mics.Count);
        Assert.Contains(antibioticIds[1], saved.Mics.Select(x => x.Id));
        Assert.Contains(antibioticIds[2], saved.Mics.Select(x => x.Id));
        Assert.DoesNotContain(antibioticIds[0], saved.Mics.Select(x => x.Id));
    }

    [Fact]
    public async Task UpdateAntibiogram_DuplicateAntibioticAcrossLists_Success()
    {
        /*
         * Unlike CreateAntibiogram, the existence check here applies Distinct() before
         * comparing counts, so reusing one antibiotic across two lists is accepted.
         * This pins that asymmetry so any change is deliberate
         */
        var (_, antibioticIds, antibiogram) = await SeedAsync(antibioticCount: 3);
        _ = antibiogram ?? throw new InvalidOperationException("Seed failure");

        var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
        {
            Id = antibiogram.Id,
            MicLevel = MinimumInhibitoryConcentration.Resistance,
            MicIds = [antibioticIds[0], antibioticIds[1]],
            FirstPriorityMedicineIds = [antibioticIds[0]],
            SecondPriorityMedicineIds = [antibioticIds[2]],
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Updated, result.StatusCode);

        var saved = await GetPersistedAntibiogramAsync(antibiogram.Id);
        Assert.Contains(antibioticIds[0], saved.Mics.Select(x => x.Id));
        Assert.Contains(antibioticIds[0], saved.FirstPriorityMedicines.Select(x => x.Id));
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task UpdateAntibiogram_AntibiogramNotFound_Fail()
    {
        // The existence check runs before the lookup, so valid antibiotic IDs are
        // needed to actually reach the antibiogram-not-found branch
        var (_, antibioticIds, _) = await SeedAsync(antibioticCount: 3, seedAntibiogram: false);
        var unknownId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new UpdateAntibiogramCommand
            {
                Id = unknownId,
                MicLevel = MinimumInhibitoryConcentration.Susceptible,
                MicIds = [antibioticIds[0]],
                FirstPriorityMedicineIds = [antibioticIds[1]],
                SecondPriorityMedicineIds = [antibioticIds[2]],
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        Assert.Equal(0, await _context.Antibiograms.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateAntibiogram_SoftDeletedAntibiogram_Fail()
    {
        // A soft-deleted antibiogram is hidden by the query filter, so it must be
        // rejected just like an unknown antibiogram
        var (_, antibioticIds, deleted) = await SeedAsync(antibioticCount: 3, softDeletedAntibiogram: true);
        _ = deleted ?? throw new InvalidOperationException("Seed failure");

        var result = await _handler.HandleAsync(
            new UpdateAntibiogramCommand
            {
                Id = deleted.Id,
                MicLevel = MinimumInhibitoryConcentration.Susceptible,
                MicIds = [antibioticIds[0]],
                FirstPriorityMedicineIds = [antibioticIds[1]],
                SecondPriorityMedicineIds = [antibioticIds[2]],
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task UpdateAntibiogram_SomeAntibioticsNotExist_Fail()
    {
        var (_, antibioticIds, antibiogram) = await SeedAsync(antibioticCount: 3);
        _ = antibiogram ?? throw new InvalidOperationException("Seed failure");
        var unknownId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new UpdateAntibiogramCommand
            {
                Id = antibiogram.Id,
                MicLevel = MinimumInhibitoryConcentration.Resistance,
                MicIds = [antibioticIds[0], unknownId],
                FirstPriorityMedicineIds = [antibioticIds[1]],
                SecondPriorityMedicineIds = [antibioticIds[2]],
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // The failed update must leave the stored relations untouched
        var saved = await GetPersistedAntibiogramAsync(antibiogram.Id);
        Assert.Equal(MinimumInhibitoryConcentration.Susceptible, saved.MicLevel);
        Assert.Equal([antibioticIds[0]], saved.Mics.ConvertAll(x => x.Id));
        Assert.Equal([antibioticIds[1]], saved.FirstPriorityMedicines.ConvertAll(x => x.Id));
        Assert.Equal([antibioticIds[2]], saved.SecondPriorityMedicines.ConvertAll(x => x.Id));

    }

    [Fact]
    public async Task UpdateAntibiogram_SoftDeletedAntibioticAmongIds_Fail()
    {
        // A soft-deleted antibiotic is hidden by the query filter, so referencing it
        // must be rejected just like an unknown antibiotic
        var (_, antibioticIds, antibiogram) = await SeedAsync(
            antibioticCount: 3, softDeletedFirstAntibiotic: true);
        _ = antibiogram ?? throw new InvalidOperationException("Seed failure");

        var result = await _handler.HandleAsync(
            new UpdateAntibiogramCommand
            {
                Id = antibiogram.Id,
                MicLevel = MinimumInhibitoryConcentration.Intermediate,
                MicIds = [antibioticIds[0]],
                FirstPriorityMedicineIds = [antibioticIds[1]],
                SecondPriorityMedicineIds = [antibioticIds[2]],
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        var saved = await GetPersistedAntibiogramAsync(antibiogram.Id);
        Assert.Equal(MinimumInhibitoryConcentration.Susceptible, saved.MicLevel);
    }

    # endregion
}
