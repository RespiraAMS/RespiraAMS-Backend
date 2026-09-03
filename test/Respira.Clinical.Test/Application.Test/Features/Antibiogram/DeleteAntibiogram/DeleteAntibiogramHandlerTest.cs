using Application.Contracts.Data;
using Application.Features.Antibiograms.DeleteAntibiogram;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.Antibiogram.DeleteAntibiogram;

using Antibiogram = Domain.Models.Antibiogram;

public class DeleteAntibiogramHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly DeleteAntibiogramHandler _handler;
    private readonly IDbContext _context;

    public DeleteAntibiogramHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var logger = new Mock<ILogger<DeleteAntibiogramHandler>>().Object;

        // Initialize handler
        _handler = new(_context, logger);
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
     * seedAntibiograms is set, a target antibiogram (with relations mic = [A0],
     * first priority = [A1], second priority = [A2]) plus a control antibiogram are
     * created so the soft delete can be told apart from untouched rows. Flags allow
     * soft-deleting the target up front to exercise the query filter
     */
    private async Task<Antibiogram> SeedAsync(int antibioticCount,
        bool seedAntibiograms = true, bool softDeletedTarget = false)
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
            });
        }
        await _context.Antibiotics.AddRangeAsync(antibiotics, TestContext.Current.CancellationToken);

        Antibiogram? target = null;
        if (seedAntibiograms)
        {
            // CLSI breakpoint: Susceptible for the target, Intermediate for the control
            target = new Antibiogram
            {
                PathogenId = pathogen.Id,
                MicLevel = MinimumInhibitoryConcentration.Susceptible,
                Mics = [antibiotics[0]],
                FirstPriorityMedicines = [antibiotics[1]],
                SecondPriorityMedicines = [antibiotics[2]],
                IsDeleted = softDeletedTarget,
                DeletedAt = softDeletedTarget ? DateTimeOffset.UtcNow : null,
            };
            var control = new Antibiogram
            {
                PathogenId = pathogen.Id,
                MicLevel = MinimumInhibitoryConcentration.Intermediate,
                Mics = [antibiotics[0]],
                FirstPriorityMedicines = [antibiotics[1]],
                SecondPriorityMedicines = [antibiotics[2]],
            };
            await _context.Antibiograms.AddRangeAsync([target, control],
                TestContext.Current.CancellationToken);
        }

        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return target ?? throw new InvalidOperationException("Seed failure");
    }

    # region Happy path

    [Fact]
    public async Task DeleteAntibiogram_WithRelations_Success()
    {
        // Upper boundary of delete: an antibiogram holding relations in all
        // three join tables
        var target = await SeedAsync(antibioticCount: 3);

        var result = await _handler.HandleAsync(new DeleteAntibiogramCommand(target.Id), TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Deleted, result.StatusCode);

        // Deletion is a soft delete: the row must remain with the flags set.
        // All entities carry a !IsDeleted query filter, so IgnoreQueryFilters is
        // required to observe them
        await using var freshContext = new AppDbContext(_options);
        var deleted = await freshContext.Antibiograms.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken);
        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.DeletedAt);
        Assert.Equal(MinimumInhibitoryConcentration.Susceptible, deleted.MicLevel);
        Assert.Equal(target.PathogenId, deleted.PathogenId);

        // The default query filter must now hide the deleted antibiogram
        Assert.False(await freshContext.Antibiograms
            .AnyAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken));

        // The control antibiogram must stay active
        var control = await freshContext.Antibiograms
            .SingleAsync(x => x.MicLevel == MinimumInhibitoryConcentration.Intermediate,
                TestContext.Current.CancellationToken);
        Assert.False(control.IsDeleted);
    }

    [Fact]
    public async Task DeleteAntibiogram_JoinRowsRemain_Success()
    {
        /*
         * Pins current behavior: the soft delete only flags the antibiograms row,
         * the three join tables keep their rows for the deleted antibiogram. The
         * query filters already hide them from any navigation load, so this is
         * consistent with the soft-delete model used everywhere else
         */
        var target = await SeedAsync(antibioticCount: 3);

        var result = await _handler.HandleAsync(new DeleteAntibiogramCommand(target.Id), TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Deleted, result.StatusCode);

        await using var db = (AppDbContext)_context;
        var micRows = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS "Value" FROM antibiogram_mic_groups
                WHERE "AntibiogramId" = {target.Id}
                """)
            .SingleAsync(TestContext.Current.CancellationToken);
        var firstPriorityRows = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS "Value" FROM antibiogram_first_priority_medicines
                WHERE "Antibiogram1Id" = {target.Id}
                """)
            .SingleAsync(TestContext.Current.CancellationToken);
        var secondPriorityRows = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS "Value" FROM antibiogram_second_priority_medicines
                WHERE "Antibiogram2Id" = {target.Id}
                """)
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, micRows);
        Assert.Equal(1, firstPriorityRows);
        Assert.Equal(1, secondPriorityRows);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task DeleteAntibiogram_NotFound_Fail()
    {
        var unknownId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new DeleteAntibiogramCommand(unknownId), TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // Nothing must exist when the target was never there
        Assert.Equal(0, await _context.Antibiograms.IgnoreQueryFilters()
            .CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAntibiogram_SoftDeletedAntibiogram_Fail()
    {
        // A soft-deleted antibiogram is already hidden by the query filter, so
        // deleting it again must be rejected just like an unknown antibiogram
        var deletedTarget = await SeedAsync(antibioticCount: 3, softDeletedTarget: true);

        var result = await _handler.HandleAsync(
            new DeleteAntibiogramCommand(deletedTarget.Id), TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // The already-deleted row must keep its original delete timestamp
        // (Postgres timestamp precision is lower than DateTimeOffset ticks)
        await using var freshContext = new AppDbContext(_options);
        var stillDeleted = await freshContext.Antibiograms.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == deletedTarget.Id, TestContext.Current.CancellationToken);
        Assert.True(stillDeleted.IsDeleted);
        Assert.NotNull(stillDeleted.DeletedAt);
        Assert.True((stillDeleted.DeletedAt - deletedTarget.DeletedAt).GetValueOrDefault().Duration()
            < TimeSpan.FromSeconds(1));
    }

    # endregion
}
