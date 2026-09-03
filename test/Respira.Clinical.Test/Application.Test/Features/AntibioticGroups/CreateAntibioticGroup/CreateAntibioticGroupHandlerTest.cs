using Application.Contracts.Data;
using Application.Features.AntibioticGroups.CreateAntibioticGroup;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.AntibioticGroups.CreateAntibioticGroup;

public class CreateAntibioticGroupHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly CreateAntibioticGroupHandler _handler;
    private readonly IDbContext _context;

    public CreateAntibioticGroupHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new CreateAntibioticGroupMapper();
        var logger = new Mock<ILogger<CreateAntibioticGroupHandler>>().Object;

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

    private async Task<AntibioticGroup> SeedGroupAsync(string name, string description, bool softDeleted = false)
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

    [Fact]
    public async Task CreateAntibioticGroup_RootGroupWithoutParent_Success()
    {
        var command = new CreateAntibioticGroupCommand
        {
            Name = "Beta-lactams",
            Description = "Cell wall synthesis inhibitors sharing the beta-lactam ring",
            ParentId = null,
        };

        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Created, result.StatusCode);
        Assert.NotNull(result.Data);

        Assert.NotEqual(Guid.Empty, result.Data.Id);

        // Verify through a fresh context so the change tracker cannot mask a failed commit
        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.AntibioticGroups.SingleAsync(
            x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);
        Assert.Equal("Beta-lactams", saved.Name);
        Assert.Equal(command.Description, saved.Description);
        Assert.Null(saved.ParentId);
    }

    [Fact]
    public async Task CreateAntibioticGroup_SubGroupWithExistingParent_Success()
    {
        var parent = await SeedGroupAsync("Beta-lactams",
            "Cell wall synthesis inhibitors sharing the beta-lactam ring");

        var result = await _handler.HandleAsync(new CreateAntibioticGroupCommand
        {
            Name = "Penicillins",
            Description = "Beta-lactam antibiotics active against gram-positive organisms",
            ParentId = parent.Id,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Created, result.StatusCode);
        Assert.NotNull(result.Data);

        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.AntibioticGroups.SingleAsync(
            x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);
        Assert.Equal(parent.Id, saved.ParentId);
        Assert.Equal("Penicillins", saved.Name);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task CreateAntibioticGroup_UnknownParent_Fail()
    {
        var unknownParentId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new CreateAntibioticGroupCommand
            {
                Name = "Cephalosporins",
                Description = "Beta-lactam antibiotics resistant to staphylococcal beta-lactamase",
                ParentId = unknownParentId,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // Nothing must be created when the parent does not exist
        Assert.Equal(0, await _context.AntibioticGroups.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAntibioticGroup_SoftDeletedParent_Fail()
    {
        // A soft-deleted parent is hidden by the query filter, so referencing it
        // must be rejected just like an unknown parent
        var deletedParent = await SeedGroupAsync("Aminoglycosides",
            "Bactericidal inhibitors of protein synthesis", softDeleted: true);

        var result = await _handler.HandleAsync(
            new CreateAntibioticGroupCommand
            {
                Name = "Gentamicin subgroup",
                Description = "Aminoglycoside used for severe gram-negative infections",
                ParentId = deletedParent.Id,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        Assert.Equal(1, await _context.AntibioticGroups.IgnoreQueryFilters()
            .CountAsync(TestContext.Current.CancellationToken));
    }

    # endregion
}
