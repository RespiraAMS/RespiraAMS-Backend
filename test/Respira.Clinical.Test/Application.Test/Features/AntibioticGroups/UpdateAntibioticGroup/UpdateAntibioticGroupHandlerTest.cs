using Application.Contracts.Data;
using Application.Features.AntibioticGroups.UpdateAntibioticGroup;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.AntibioticGroups.UpdateAntibioticGroup;

public class UpdateAntibioticGroupHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly UpdateAntibioticGroupHandler _handler;
    private readonly IDbContext _context;

    public UpdateAntibioticGroupHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new UpdateAntibioticGroupMapper();
        var logger = new Mock<ILogger<UpdateAntibioticGroupHandler>>().Object;

        // Initialize handler
        _handler = new(_context, mapper, logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        // Children reference parents through a self FK, so delete them first.
        // IgnoreQueryFilters is needed because soft-deleted rows are hidden by the
        // query filter but still occupy the table
        await _context.AntibioticGroups.IgnoreQueryFilters()
            .Where(x => x.ParentId != null)
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.AntibioticGroups.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    private async Task<AntibioticGroup> SeedGroupAsync(string name, string description,
        bool softDeleted = false)
    {
        var group = new AntibioticGroup
        {
            Name = name,
            Description = description,
            ParentId = null,
            IsDeleted = softDeleted,
            DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
        };
        await _context.AntibioticGroups.AddAsync(group, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return group;
    }

    # region Happy path

    [Theory]
    [InlineData("Aminoglycosides", "Bactericidal inhibitors of protein synthesis used for severe gram-negative infections")]
    [InlineData("Glycopeptides", "Inhibitors of cell wall synthesis active against gram-positive bacteria")]
    public async Task UpdateAntibioticGroup_RootGroup_Success(string newName, string newDescription)
    {
        var seeded = await SeedGroupAsync("Old group", "Outdated description");
        var updatedBefore = DateTimeOffset.UtcNow;

        var result = await _handler.HandleAsync(new UpdateAntibioticGroupCommand
        {
            Id = seeded.Id,
            Name = newName,
            Description = newDescription,
            ParentId = null,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Updated, result.StatusCode);

        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.AntibioticGroups.SingleAsync(
            x => x.Id == seeded.Id, TestContext.Current.CancellationToken);
        Assert.Equal(newName, saved.Name);
        Assert.Equal(newDescription, saved.Description);
        Assert.Null(saved.ParentId);
        Assert.InRange(saved.UpdatedAt.ToUnixTimeMilliseconds(),
            updatedBefore.AddSeconds(-5).ToUnixTimeMilliseconds(),
            DateTimeOffset.UtcNow.AddSeconds(5).ToUnixTimeMilliseconds());
    }

    [Fact]
    public async Task UpdateAntibioticGroup_ReparentUnderExistingGroup_Success()
    {
        var parent = await SeedGroupAsync("Beta-lactams",
            "Cell wall synthesis inhibitors sharing the beta-lactam ring");
        var seeded = await SeedGroupAsync("Penicillins",
            "Standalone antibiotic group without a parent");

        var result = await _handler.HandleAsync(new UpdateAntibioticGroupCommand
        {
            Id = seeded.Id,
            Name = "Penicillins",
            Description = "Subgroup of beta-lactam antibiotics",
            ParentId = parent.Id,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Updated, result.StatusCode);

        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.AntibioticGroups.SingleAsync(
            x => x.Id == seeded.Id, TestContext.Current.CancellationToken);
        Assert.Equal(parent.Id, saved.ParentId);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task UpdateAntibioticGroup_GroupNotFound_Fail()
    {
        var unknownId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new UpdateAntibioticGroupCommand
            {
                Id = unknownId,
                Name = "Lincosamides",
                Description = "Should never be written",
                ParentId = null,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        Assert.Equal(0, await _context.AntibioticGroups.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateAntibioticGroup_UnknownParent_Fail()
    {
        var seeded = await SeedGroupAsync("Penicillins", "Existing group");
        var unknownParentId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new UpdateAntibioticGroupCommand
            {
                Id = seeded.Id,
                Name = "Penicillins",
                Description = "Reparent attempt with an unknown parent",
                ParentId = unknownParentId,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // The target must stay untouched when the parent does not exist
        await using var freshContext = new AppDbContext(_options);
        var untouched = await freshContext.AntibioticGroups.SingleAsync(
            x => x.Id == seeded.Id, TestContext.Current.CancellationToken);
        Assert.Null(untouched.ParentId);
        Assert.Equal("Existing group", untouched.Description);
    }

    [Fact]
    public async Task UpdateAntibioticGroup_SoftDeletedParent_Fail()
    {
        // A soft-deleted parent is hidden by the query filter, so referencing it
        // must be rejected just like an unknown parent
        var deletedParent = await SeedGroupAsync("Polypeptides",
            "Discontinued classification branch", softDeleted: true);
        var seeded = await SeedGroupAsync("Colistin subgroup", "Existing group");

        var result = await _handler.HandleAsync(
            new UpdateAntibioticGroupCommand
            {
                Id = seeded.Id,
                Name = "Colistin subgroup",
                Description = "Reparent under a deleted branch",
                ParentId = deletedParent.Id,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    # endregion
}
