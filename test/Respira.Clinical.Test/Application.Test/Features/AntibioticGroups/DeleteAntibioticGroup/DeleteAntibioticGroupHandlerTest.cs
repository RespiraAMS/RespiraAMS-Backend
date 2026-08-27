using Application.Contracts.Data;
using Application.Features.AntibioticGroups.DeleteAntibioticGroup;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Exceptions;

namespace Application.Test.Features.AntibioticGroups.DeleteAntibioticGroup;

public class DeleteAntibioticGroupHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly DeleteAntibioticGroupHandler _handler;
    private readonly IDbContext _context;

    public DeleteAntibioticGroupHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var logger = new Mock<ILogger<DeleteAntibioticGroupHandler>>().Object;

        // Initialize handler
        _handler = new(_context, logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        // Antibiotics reference groups through an FK, so delete them first.
        // IgnoreQueryFilters is needed because soft-deleted rows are hidden by the
        // query filter but still occupy the table
        await _context.Antibiotics.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.AntibioticGroups.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    /*
     * Seeds a target group and a control group. When withAntibiotics is set, the
     * target gets two antibiotics and the control group one, so the cascade can be
     * told apart from untouched rows
     */
    private async Task<(AntibioticGroup Target, AntibioticGroup Other)> SeedAsync(bool withAntibiotics)
    {
        var target = new AntibioticGroup
        {
            Name = "Beta-lactams",
            Description = "Cell wall synthesis inhibitors sharing the beta-lactam ring",
            ParentId = null,
        };
        var other = new AntibioticGroup
        {
            Name = "Macrolides",
            Description = "Protein synthesis inhibitors with a macrocyclic lactone ring",
            ParentId = null,
        };

        await _context.AntibioticGroups.AddRangeAsync([target, other], TestContext.Current.CancellationToken);

        if (withAntibiotics)
        {
            await _context.Antibiotics.AddRangeAsync(
                [
                    new Antibiotic
                    {
                        Name = "Amoxicillin",
                        AntibioticGroupId = target.Id,
                        Classification = AwareClassification.Access,
                    },
                    new Antibiotic
                    {
                        Name = "Meropenem",
                        AntibioticGroupId = target.Id,
                        Classification = AwareClassification.Watch,
                    },
                    new Antibiotic
                    {
                        Name = "Azithromycin",
                        AntibioticGroupId = other.Id,
                        Classification = AwareClassification.Watch,
                    },
                ], TestContext.Current.CancellationToken);
        }

        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (target, other);
    }

    # region Happy path

    [Fact]
    public async Task DeleteAntibioticGroup_WithAntibiotics_CascadesSoftDelete_Success()
    {
        var (target, other) = await SeedAsync(withAntibiotics: true);

        await _handler.HandleAsync(new DeleteAntibioticGroupCommand { Id = target.Id },
            TestContext.Current.CancellationToken);

        // All entities carry a !IsDeleted query filter, so IgnoreQueryFilters is
        // required to observe the soft-delete flags
        await using var freshContext = new AppDbContext(_options);

        var deletedGroup = await freshContext.AntibioticGroups.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken);
        Assert.True(deletedGroup.IsDeleted);
        Assert.NotNull(deletedGroup.DeletedAt);

        var deletedAntibiotics = await freshContext.Antibiotics.IgnoreQueryFilters()
            .Where(x => x.AntibioticGroupId == target.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, deletedAntibiotics.Count);
        Assert.All(deletedAntibiotics, x =>
        {
            Assert.True(x.IsDeleted);
            Assert.NotNull(x.DeletedAt);
        });

        // The control group and its antibiotic must stay untouched
        Assert.False(await freshContext.AntibioticGroups.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == other.Id && x.IsDeleted, TestContext.Current.CancellationToken));
        Assert.Equal(1, await freshContext.Antibiotics.IgnoreQueryFilters()
            .CountAsync(x => x.AntibioticGroupId == other.Id && !x.IsDeleted, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAntibioticGroup_NoAntibiotics_Success()
    {
        // Lower boundary of the cascade: nothing linked to the group
        var (target, _) = await SeedAsync(withAntibiotics: false);

        await _handler.HandleAsync(new DeleteAntibioticGroupCommand { Id = target.Id },
            TestContext.Current.CancellationToken);

        await using var freshContext = new AppDbContext(_options);
        var deleted = await freshContext.AntibioticGroups.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken);

        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.DeletedAt);
        Assert.True(await freshContext.AntibioticGroups.IgnoreQueryFilters()
            .AnyAsync(x => !x.IsDeleted, TestContext.Current.CancellationToken));
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task DeleteAntibioticGroup_GroupNotFound_Fail()
    {
        var unknownId = Guid.CreateVersion7();

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(
            new DeleteAntibioticGroupCommand { Id = unknownId }, TestContext.Current.CancellationToken));

        // Nothing must be soft-deleted when the target does not exist
        Assert.Equal(0, await _context.AntibioticGroups.IgnoreQueryFilters()
            .CountAsync(TestContext.Current.CancellationToken));
    }

    # endregion
}
