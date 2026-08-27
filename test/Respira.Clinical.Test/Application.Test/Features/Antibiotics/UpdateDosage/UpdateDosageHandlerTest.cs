using Application.Contracts.Data;
using Application.Features.Antibiotics.UpdateDosage;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Range = Domain.Models.Range;
using Respira.ServiceDefaults.Exceptions;

namespace Application.Test.Features.Antibiotics.UpdateDosage;

public class UpdateDosageHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly UpdateDosageHandler _handler;
    private readonly IDbContext _context;

    public UpdateDosageHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new UpdateDosageMapper();
        var logger = new Mock<ILogger<UpdateDosageHandler>>().Object;

        // Initialize handler
        _handler = new(_context, mapper, logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        // Dosages reference antibiotics through an FK, so delete them first.
        // IgnoreQueryFilters is needed because soft-deleted rows are hidden by the
        // query filter but still occupy the table
        await _context.Dosages.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Antibiotics.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.AntibioticGroups.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    /*
     * Seeds a group, the target antibiotic and an optional other antibiotic holding
     * initialDosages. The DosageIds primitive collection is kept in sync manually
     * since EF Core does not maintain it
     */
    private async Task<Antibiotic> SeedAsync(
        string antibioticName,
        List<(string Dose, RouteOfAdministration Route, Range? Crcl)> initialDosages,
        bool softDeleted = false,
        bool seedOtherAntibiotic = false)
    {
        var group = new AntibioticGroup
        {
            Name = "Beta-lactams",
            Description = "Cell wall synthesis inhibitors sharing the beta-lactam ring",
            ParentId = null,
        };
        await _context.AntibioticGroups.AddAsync(group, TestContext.Current.CancellationToken);

        var antibiotic = new Antibiotic
        {
            Name = antibioticName,
            AntibioticGroupId = group.Id,
            Classification = AwareClassification.Access,
            IsDeleted = softDeleted,
            DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
        };
        foreach (var (dose, route, crcl) in initialDosages)
        {
            var dosage = new Dosage
            {
                AntibioticId = antibiotic.Id,
                Dose = dose,
                RouteOfAdministration = route,
                Crcl = crcl,
            };
            antibiotic.Dosages.Add(dosage);
            antibiotic.DosageIds.Add(dosage.Id);
        }
        await _context.Antibiotics.AddAsync(antibiotic, TestContext.Current.CancellationToken);

        if (seedOtherAntibiotic)
        {
            var other = new Antibiotic
            {
                Name = $"{antibioticName} (other)",
                AntibioticGroupId = group.Id,
                Classification = AwareClassification.Watch,
            };
            var otherDosage = new Dosage
            {
                AntibioticId = other.Id,
                Dose = "300 mg orally every 6 hours",
                RouteOfAdministration = RouteOfAdministration.Oral,
                Crcl = null,
            };
            other.Dosages.Add(otherDosage);
            other.DosageIds.Add(otherDosage.Id);
            await _context.Antibiotics.AddAsync(other, TestContext.Current.CancellationToken);
        }

        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return antibiotic;
    }

    private async Task<Dosage> GetPersistedDosageAsync(Guid dosageId)
    {
        await using var freshContext = new AppDbContext(_options);
        return await freshContext.Dosages.SingleAsync(
            x => x.Id == dosageId, TestContext.Current.CancellationToken);
    }

    # region Happy path

    [Fact]
    public async Task UpdateDosage_StandardDoseTextChanged_Success()
    {
        var antibiotic = await SeedAsync("Amoxicillin",
        [
            ("500 mg orally every 8 hours", RouteOfAdministration.Oral, null),
        ]);
        var target = antibiotic.Dosages[0];

        await _handler.HandleAsync(new UpdateDosageCommand
        {
            Id = target.Id,
            AntibioticId = antibiotic.Id,
            RouteOfAdministration = RouteOfAdministration.Oral,
            Dose = "875 mg orally every 12 hours",
            Crcl = null,
        }, TestContext.Current.CancellationToken);

        // Verify through a fresh context so the change tracker cannot mask a failed commit
        var saved = await GetPersistedDosageAsync(target.Id);
        Assert.Equal("875 mg orally every 12 hours", saved.Dose);
        Assert.Equal(RouteOfAdministration.Oral, saved.RouteOfAdministration);
        Assert.Null(saved.Crcl);
        Assert.True(saved.UpdatedAt >= saved.CreatedAt);

        // The dosage must stay linked to its antibiotic
        await using var linkContext = new AppDbContext(_options);
        var savedAntibiotic = await linkContext.Antibiotics.SingleAsync(
            x => x.Id == antibiotic.Id, TestContext.Current.CancellationToken);
        Assert.Contains(target.Id, savedAntibiotic.DosageIds);
    }

    [Fact]
    public async Task UpdateDosage_AdjustedDoseToNonOverlappingRange_Success()
    {
        /*
         * Boundary value technique: existing range [30, 45] and updated range
         * (45, 60] only touch at 45 without sharing it, which is not an overlap
         * and therefore must be accepted
         */
        var antibiotic = await SeedAsync("Meropenem",
        [
            ("1 g IV every 8 hours", RouteOfAdministration.Intravenous, null),
            (
                "500 mg IV every 12 hours",
                RouteOfAdministration.Intravenous,
                new Range { Min = 30, IsMinExclusive = false, Max = 45, IsMaxExclusive = false, Unit = "mL/min" }
            ),
            (
                "500 mg IV every 24 hours",
                RouteOfAdministration.Intravenous,
                new Range { Min = 60, IsMinExclusive = false, Max = 75, IsMaxExclusive = false, Unit = "mL/min" }
            ),
        ]);
        var adjusted = antibiotic.Dosages[2];

        await _handler.HandleAsync(new UpdateDosageCommand
        {
            Id = adjusted.Id,
            AntibioticId = antibiotic.Id,
            RouteOfAdministration = RouteOfAdministration.Intravenous,
            Dose = "500 mg IV every 24 hours",
            Crcl = new Range { Min = 45, IsMinExclusive = true, Max = 60, IsMaxExclusive = false, Unit = "mL/min" },
        }, TestContext.Current.CancellationToken);

        var saved = await GetPersistedDosageAsync(adjusted.Id);
        Assert.Equal("500 mg IV every 24 hours", saved.Dose);
        Assert.NotNull(saved.Crcl);
        Assert.Equal(45, saved.Crcl.Min);
        Assert.Equal(60, saved.Crcl.Max);
        Assert.True(saved.Crcl.IsMinExclusive);
        Assert.False(saved.Crcl.IsMaxExclusive);
    }

    [Fact]
    public async Task UpdateDosage_ChangeRouteWithSingleStandardDose_Success()
    {
        // Moving the only standard dose from Oral to Intravenous keeps exactly one
        // standard dose per route, so it must be accepted
        var antibiotic = await SeedAsync("Ciprofloxacin",
        [
            ("500 mg orally every 12 hours", RouteOfAdministration.Oral, null),
        ]);
        var target = antibiotic.Dosages[0];
        var updatedAtBefore = target.UpdatedAt;

        await _handler.HandleAsync(new UpdateDosageCommand
        {
            Id = target.Id,
            AntibioticId = antibiotic.Id,
            RouteOfAdministration = RouteOfAdministration.Intravenous,
            Dose = "400 mg IV every 12 hours",
            Crcl = null,
        }, TestContext.Current.CancellationToken);

        var saved = await GetPersistedDosageAsync(target.Id);
        Assert.Equal(RouteOfAdministration.Intravenous, saved.RouteOfAdministration);
        Assert.Equal("400 mg IV every 12 hours", saved.Dose);
        Assert.True(saved.UpdatedAt > updatedAtBefore);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task UpdateDosage_AntibioticNotFound_Fail()
    {
        var unknownId = Guid.CreateVersion7();

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(
            new UpdateDosageCommand
            {
                Id = Guid.CreateVersion7(),
                AntibioticId = unknownId,
                RouteOfAdministration = RouteOfAdministration.Oral,
                Dose = "500 mg orally every 8 hours",
                Crcl = null,
            }, TestContext.Current.CancellationToken));

        Assert.Equal(0, await _context.Dosages.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateDosage_SoftDeletedAntibiotic_Fail()
    {
        // A soft-deleted antibiotic is hidden by the query filter, so it must be
        // rejected just like an unknown antibiotic
        var deletedAntibiotic = await SeedAsync("Chloramphenicol",
        [
            ("500 mg orally every 6 hours", RouteOfAdministration.Oral, null),
        ], softDeleted: true);

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(
            new UpdateDosageCommand
            {
                Id = deletedAntibiotic.Dosages[0].Id,
                AntibioticId = deletedAntibiotic.Id,
                RouteOfAdministration = RouteOfAdministration.Oral,
                Dose = "250 mg orally every 6 hours",
                Crcl = null,
            }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateDosage_DosageNotOwnedByAntibiotic_Fail()
    {
        // A real dosage ID that belongs to a different antibiotic must be rejected:
        // dosages can only be updated through their owning antibiotic
        var antibiotic = await SeedAsync("Amoxicillin",
        [
            ("500 mg orally every 8 hours", RouteOfAdministration.Oral, null),
        ], seedOtherAntibiotic: true);
        var foreignDosageId = (await _context.Antibiotics
            .Include(x => x.Dosages)
            .SingleAsync(x => x.Name.Contains("(other)"), TestContext.Current.CancellationToken)).Dosages[0].Id;

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(
            new UpdateDosageCommand
            {
                Id = foreignDosageId,
                AntibioticId = antibiotic.Id,
                RouteOfAdministration = RouteOfAdministration.Oral,
                Dose = "875 mg orally every 12 hours",
                Crcl = null,
            }, TestContext.Current.CancellationToken));

        // The foreign dosage must stay untouched
        var saved = await GetPersistedDosageAsync(foreignDosageId);
        Assert.Equal("300 mg orally every 6 hours", saved.Dose);
    }

    [Fact]
    public async Task UpdateDosage_MakesSecondStandardDoseForRoute_Fail()
    {
        /*
         * Business rule: each route of administration must have 1 and only 1 standard
         * dose. Clearing the CrCl of the oral adjusted dose would leave two oral
         * standard doses, which must be rejected
         */
        var antibiotic = await SeedAsync("Ciprofloxacin",
        [
            ("500 mg orally every 12 hours", RouteOfAdministration.Oral, null),
            (
                "250 mg orally every 24 hours",
                RouteOfAdministration.Oral,
                new Range { Min = 15, IsMinExclusive = true, Max = 29, IsMaxExclusive = false, Unit = "mL/min" }
            ),
        ]);
        var adjusted = antibiotic.Dosages[1];

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(
            new UpdateDosageCommand
            {
                Id = adjusted.Id,
                AntibioticId = antibiotic.Id,
                RouteOfAdministration = RouteOfAdministration.Oral,
                Dose = "250 mg orally every 24 hours",
                Crcl = null,
            }, TestContext.Current.CancellationToken));

        // The rejected update must not be persisted
        var saved = await GetPersistedDosageAsync(adjusted.Id);
        Assert.NotNull(saved.Crcl);
        Assert.Equal("250 mg orally every 24 hours", saved.Dose);
    }

    [Fact]
    public async Task UpdateDosage_CreatesOverlappingCrclRange_Fail()
    {
        /*
         * Business rule: within one route, CrCl ranges must not overlap.
         * Boundary value technique: moving [30, 45] to [45, 60] makes it touch the
         * existing [60, 75] at the exact point 60 (both inclusive), which counts as
         * an overlap and must be rejected
         */
        var antibiotic = await SeedAsync("Meropenem",
        [
            ("1 g IV every 8 hours", RouteOfAdministration.Intravenous, null),
            (
                "500 mg IV every 12 hours",
                RouteOfAdministration.Intravenous,
                new Range { Min = 30, IsMinExclusive = false, Max = 45, IsMaxExclusive = false, Unit = "mL/min" }
            ),
            (
                "500 mg IV every 24 hours",
                RouteOfAdministration.Intravenous,
                new Range { Min = 60, IsMinExclusive = false, Max = 75, IsMaxExclusive = false, Unit = "mL/min" }
            ),
        ]);
        var adjusted = antibiotic.Dosages[1];

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(
            new UpdateDosageCommand
            {
                Id = adjusted.Id,
                AntibioticId = antibiotic.Id,
                RouteOfAdministration = RouteOfAdministration.Intravenous,
                Dose = "500 mg IV every 12 hours",
                Crcl = new Range { Min = 45, IsMinExclusive = false, Max = 60, IsMaxExclusive = false, Unit = "mL/min" },
            }, TestContext.Current.CancellationToken));

        // The old range must remain persisted untouched
        var saved = await GetPersistedDosageAsync(adjusted.Id);
        Assert.NotNull(saved.Crcl);
        Assert.Equal(30, saved.Crcl.Min);
        Assert.Equal(45, saved.Crcl.Max);
        Assert.False(saved.Crcl.IsMinExclusive);
    }

    // Skipped paths: the two ServerException branches (SaveChangesAsync returning <= 0
    // and an unexpected exception escaping DosageBusinessChecker) cannot be simulated
    // against a real database without mocking infrastructure internals

    # endregion
}
