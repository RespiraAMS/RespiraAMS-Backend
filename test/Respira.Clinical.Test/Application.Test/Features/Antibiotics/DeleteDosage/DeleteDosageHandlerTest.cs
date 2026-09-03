using Application.Contracts.Data;
using Application.Features.Antibiotics.DeleteDosage;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Range = Domain.Models.Range;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.Antibiotics.DeleteDosage;

public class DeleteDosageHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly DeleteDosageHandler _handler;
    private readonly IDbContext _context;

    public DeleteDosageHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var logger = new Mock<ILogger<DeleteDosageHandler>>().Object;

        // Initialize handler
        _handler = new(_context, logger);
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
     * Seeds a group, the target antibiotic holding initialDosages and an optional
     * control antibiotic with one dosage of its own. The DosageIds primitive collection
     * is kept in sync manually since EF Core does not maintain it
     */
    private async Task<Antibiotic> SeedAsync(string antibioticName,
        List<(string Dose, RouteOfAdministration Route, Range? Crcl)> initialDosages,
        bool softDeleted = false,
        bool seedControlAntibiotic = false)
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

        if (seedControlAntibiotic)
        {
            var control = new Antibiotic
            {
                Name = "Azithromycin",
                AntibioticGroupId = group.Id,
                Classification = AwareClassification.Watch,
            };
            var controlDosage = new Dosage
            {
                AntibioticId = control.Id,
                Dose = "250 mg orally once daily",
                RouteOfAdministration = RouteOfAdministration.Oral,
                Crcl = null,
            };
            control.Dosages.Add(controlDosage);
            control.DosageIds.Add(controlDosage.Id);
            await _context.Antibiotics.AddAsync(control, TestContext.Current.CancellationToken);
        }

        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return antibiotic;
    }

    private async Task<Dosage?> FindPersistedDosageIncludingDeletedAsync(Guid dosageId)
    {
        await using var freshContext = new AppDbContext(_options);
        return await freshContext.Dosages.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == dosageId, TestContext.Current.CancellationToken);
    }

    private async Task<Antibiotic> GetPersistedAntibioticAsync(Guid antibioticId)
    {
        await using var freshContext = new AppDbContext(_options);
        return await freshContext.Antibiotics.SingleAsync(
            x => x.Id == antibioticId, TestContext.Current.CancellationToken);
    }

    # region Happy path

    [Fact]
    public async Task DeleteDosage_AdjustedDose_Success()
    {
        // Deleting the only renal-adjusted oral dose keeps the oral standard dose,
        // which still satisfies the business rules
        var antibiotic = await SeedAsync("Ciprofloxacin",
        [
            ("500 mg orally every 12 hours", RouteOfAdministration.Oral, null),
            (
                "250 mg orally every 24 hours",
                RouteOfAdministration.Oral,
                new Range { Min = 15, IsMinExclusive = true, Max = 29, IsMaxExclusive = false, Unit = "mL/min" }
            ),
        ], seedControlAntibiotic: true);
        var target = antibiotic.Dosages[1];

        var result = await _handler.HandleAsync(new DeleteDosageCommand
        {
            Id = target.Id,
            AntibioticId = antibiotic.Id,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Deleted, result.StatusCode);

        var saved = await FindPersistedDosageIncludingDeletedAsync(target.Id);
        Assert.NotNull(saved);
        Assert.True(saved.IsDeleted);
        Assert.NotNull(saved.DeletedAt);

        // The default query filter must now hide the deleted dosage
        await using var filteredContext = new AppDbContext(_options);
        Assert.False(await filteredContext.Dosages
            .AnyAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken));

        // The control antibiotic's dosage must stay untouched and active
        Assert.Equal(1, await filteredContext.Dosages
            .CountAsync(x => !x.IsDeleted && x.AntibioticId != antibiotic.Id,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteDosage_OneAdjustedDoseAmongSeveral_Success()
    {
        /*
         * Deleting one renal-adjusted IV dose out of two keeps the standard dose plus
         * the other adjusted dose; their CrCl ranges [30, 45] and [60, 75] remain
         * non-overlapping so the business rules still hold
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
        var target = antibiotic.Dosages[2];

        var result = await _handler.HandleAsync(new DeleteDosageCommand
        {
            Id = target.Id,
            AntibioticId = antibiotic.Id,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Deleted, result.StatusCode);

        var deleted = await FindPersistedDosageIncludingDeletedAsync(target.Id);
        Assert.NotNull(deleted);
        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.DeletedAt);

        var remaining = await FindPersistedDosageIncludingDeletedAsync(antibiotic.Dosages[0].Id);
        Assert.NotNull(remaining);
        Assert.False(remaining.IsDeleted);
        Assert.Null(remaining.DeletedAt);

        remaining = await FindPersistedDosageIncludingDeletedAsync(antibiotic.Dosages[1].Id);
        Assert.NotNull(remaining);
        Assert.False(remaining.IsDeleted);
        Assert.Null(remaining.DeletedAt);

    }

    # endregion

    # region Fail path

    [Fact]
    public async Task DeleteDosage_AntibioticNotFound_Fail()
    {
        var unknownId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new DeleteDosageCommand
            {
                Id = Guid.CreateVersion7(),
                AntibioticId = unknownId,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // Nothing must be soft-deleted when the antibiotic does not exist
        Assert.Equal(0, await _context.Dosages.IgnoreQueryFilters()
            .CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteDosage_SoftDeletedAntibiotic_Fail()
    {
        // A soft-deleted antibiotic is hidden by the query filter, so it must be
        // rejected just like an unknown antibiotic
        var deletedAntibiotic = await SeedAsync("Chloramphenicol",
        [
            ("500 mg orally every 6 hours", RouteOfAdministration.Oral, null),
        ], softDeleted: true);

        var result = await _handler.HandleAsync(
            new DeleteDosageCommand
            {
                Id = deletedAntibiotic.Dosages[0].Id,
                AntibioticId = deletedAntibiotic.Id,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task DeleteDosage_DosageNotOwnedByAntibiotic_Fail()
    {
        // A real dosage ID that belongs to a different antibiotic must be rejected:
        // dosages can only be deleted through their owning antibiotic
        var antibiotic = await SeedAsync("Amoxicillin",
        [
            ("500 mg orally every 8 hours", RouteOfAdministration.Oral, null),
        ], seedControlAntibiotic: true);
        var control = await _context.Antibiotics
            .Include(x => x.Dosages)
            .SingleAsync(x => x.Name == "Azithromycin", TestContext.Current.CancellationToken);
        var foreignDosageId = control.Dosages[0].Id;

        var result = await _handler.HandleAsync(
            new DeleteDosageCommand
            {
                Id = foreignDosageId,
                AntibioticId = antibiotic.Id,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // The foreign dosage must stay active
        var saved = await FindPersistedDosageIncludingDeletedAsync(foreignDosageId);
        Assert.NotNull(saved);
        Assert.False(saved.IsDeleted);
        Assert.Null(saved.DeletedAt);
    }

    [Fact]
    public async Task DeleteDosage_LastRemainingDosage_Fail()
    {
        /*
         * Business rule: an antibiotic must keep at least 1 dosage regardless of
         * route. Lower boundary: exactly one dosage left, deleting it is rejected
         */
        var antibiotic = await SeedAsync("Amoxicillin",
        [
            ("500 mg orally every 8 hours", RouteOfAdministration.Oral, null),
        ]);

        var result = await _handler.HandleAsync(
            new DeleteDosageCommand
            {
                Id = antibiotic.Dosages[0].Id,
                AntibioticId = antibiotic.Id,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BusinessRuleViolation, result.StatusCode);

        // The rejected deletion must leave the dosage active and linked
        var saved = await FindPersistedDosageIncludingDeletedAsync(antibiotic.Dosages[0].Id);
        Assert.NotNull(saved);
        Assert.False(saved.IsDeleted);
        Assert.Null(saved.DeletedAt);
        var savedAntibiotic = await GetPersistedAntibioticAsync(antibiotic.Id);
        Assert.Contains(antibiotic.Dosages[0].Id, savedAntibiotic.DosageIds);
    }

    [Fact]
    public async Task DeleteDosage_OnlyStandardDoseForRoute_Fail()
    {
        /*
         * Business rule: each route present in the dosage list needs exactly one
         * standard dose. Removing the oral standard leaves the oral route with zero
         * standard doses while its adjusted dose remains, so it must be rejected
         */
        var antibiotic = await SeedAsync("Ciprofloxacin",
        [
            ("500 mg orally every 12 hours", RouteOfAdministration.Oral, null),
            (
                "250 mg orally every 24 hours",
                RouteOfAdministration.Oral,
                new Range { Min = 15, IsMinExclusive = true, Max = 29, IsMaxExclusive = false, Unit = "mL/min" }
            ),
            ("400 mg IV every 12 hours", RouteOfAdministration.Intravenous, null),
        ]);
        var oralStandard = antibiotic.Dosages[0];

        var result = await _handler.HandleAsync(
            new DeleteDosageCommand
            {
                Id = oralStandard.Id,
                AntibioticId = antibiotic.Id,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BusinessRuleViolation, result.StatusCode);

        // All three dosages must stay active and linked after the rejection
        var savedAntibiotic = await GetPersistedAntibioticAsync(antibiotic.Id);
        Assert.Equal(3, savedAntibiotic.DosageIds.Count);
        foreach (var dosage in antibiotic.Dosages)
        {
            var saved = await FindPersistedDosageIncludingDeletedAsync(dosage.Id);
            Assert.NotNull(saved);
            Assert.False(saved.IsDeleted);
            Assert.Null(saved.DeletedAt);
        }
    }

    # endregion
}
