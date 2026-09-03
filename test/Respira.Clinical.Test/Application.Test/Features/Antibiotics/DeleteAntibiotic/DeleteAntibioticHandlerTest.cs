using Application.Contracts.Data;
using Application.Features.Antibiotics.DeleteAntibiotic;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.ServiceDefaults.Exceptions;

namespace Application.Test.Features.Antibiotics.DeleteAntibiotic;

public class DeleteAntibioticHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly DeleteAntibioticHandler _handler;
    private readonly IDbContext _context;

    public DeleteAntibioticHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var logger = new Mock<ILogger<DeleteAntibioticHandler>>().Object;

        // Initialize handler
        _handler = new(_context, logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        // Dosages and antibiotics reference groups through FKs, so delete them first.
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
     * Seeds a target antibiotic with two dosages and a control antibiotic (in another
     * group) with one dosage, so the cascade can be told apart from untouched rows
     */
    private async Task<Antibiotic> SeedAsync(bool withDosages)
    {
        var targetGroup = new AntibioticGroup
        {
            Name = "Beta-lactams",
            Description = "Cell wall synthesis inhibitors sharing the beta-lactam ring",
            ParentId = null,
        };
        var controlGroup = new AntibioticGroup
        {
            Name = "Macrolides",
            Description = "Protein synthesis inhibitors with a macrocyclic lactone ring",
            ParentId = null,
        };
        await _context.AntibioticGroups.AddRangeAsync([targetGroup, controlGroup],
            TestContext.Current.CancellationToken);

        var target = new Antibiotic
        {
            Name = "Amoxicillin",
            AntibioticGroupId = targetGroup.Id,
            Classification = AwareClassification.Access,
        };
        var control = new Antibiotic
        {
            Name = "Azithromycin",
            AntibioticGroupId = controlGroup.Id,
            Classification = AwareClassification.Watch,
        };
        await _context.Antibiotics.AddRangeAsync([target, control], TestContext.Current.CancellationToken);

        if (withDosages)
        {
            await _context.Dosages.AddRangeAsync(
                [
                    new Dosage
                    {
                        AntibioticId = target.Id,
                        RouteOfAdministration = RouteOfAdministration.Oral,
                        Dose = "500 mg orally every 8 hours",
                        Crcl = null,
                    },
                    new Dosage
                    {
                        AntibioticId = target.Id,
                        RouteOfAdministration = RouteOfAdministration.Intravenous,
                        Dose = "1 g IV every 8 hours",
                        Crcl = null,
                    },
                    new Dosage
                    {
                        AntibioticId = control.Id,
                        RouteOfAdministration = RouteOfAdministration.Oral,
                        Dose = "250 mg orally once daily",
                        Crcl = null,
                    },
                ], TestContext.Current.CancellationToken);
        }

        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return target;
    }

    # region Happy path

    [Fact]
    public async Task DeleteAntibiotic_WithDosages_CascadesSoftDelete_Success()
    {
        var target = await SeedAsync(withDosages: true);

        var result = await _handler.HandleAsync(new DeleteAntibioticCommand(target.Id), TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Deleted, result.StatusCode);

        // All entities carry a !IsDeleted query filter, so IgnoreQueryFilters is
        // required to observe the soft-delete flags
        await using var freshContext = new AppDbContext(_options);

        var deletedAntibiotic = await freshContext.Antibiotics.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken);
        Assert.True(deletedAntibiotic.IsDeleted);
        Assert.NotNull(deletedAntibiotic.DeletedAt);

        // Cascade: every dosage of the deleted antibiotic
        var deletedDosages = await freshContext.Dosages.IgnoreQueryFilters()
            .Where(x => x.AntibioticId == target.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, deletedDosages.Count);
        Assert.All(deletedDosages, x =>
        {
            Assert.True(x.IsDeleted);
            Assert.NotNull(x.DeletedAt);
        });

        // The control antibiotic and its dosage must stay untouched
        Assert.False(await freshContext.Antibiotics.IgnoreQueryFilters()
            .AnyAsync(x => x.Name == "Azithromycin" && x.IsDeleted, TestContext.Current.CancellationToken));
        Assert.Equal(1, await freshContext.Dosages.IgnoreQueryFilters()
            .CountAsync(x => !x.IsDeleted, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAntibiotic_NoDosages_Success()
    {
        // Lower boundary of the cascade: no dosage linked to the antibiotic
        var target = await SeedAsync(withDosages: false);

        var result = await _handler.HandleAsync(new DeleteAntibioticCommand(target.Id),
            TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Deleted, result.StatusCode);

        await using var freshContext = new AppDbContext(_options);
        var deleted = await freshContext.Antibiotics.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken);

        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.DeletedAt);
        Assert.True(await freshContext.Antibiotics.IgnoreQueryFilters()
            .AnyAsync(x => !x.IsDeleted, TestContext.Current.CancellationToken));
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task DeleteAntibiotic_AntibioticNotFound_Fail()
    {
        var unknownId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new DeleteAntibioticCommand(unknownId), TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // Nothing must be soft-deleted when the target does not exist
        Assert.Equal(0, await _context.Antibiotics.IgnoreQueryFilters()
            .CountAsync(TestContext.Current.CancellationToken));
    }

    # endregion
}
