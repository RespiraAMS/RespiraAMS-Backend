using Application.Contracts.Data;
using Application.Features.AntibioticGroups.GetAntibioticGroups;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.AntibioticGroups.GetAntibioticGroups;

public class GetAntibioticGroupsHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly GetAntibioticGroupsHandler _handler;
    private readonly IDbContext _context;

    public GetAntibioticGroupsHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);

        // Initialize handler
        _handler = new(_context);
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

    # region Happy path

    [Fact]
    public async Task GetAntibioticGroups_ReturnsAllSortedByNameAscending_Success()
    {
        // Inserted deliberately out of alphabetical order, including one subgroup
        var seeded = new List<AntibioticGroup>
        {
            new() { Name = "Macrolides", Description = "Macrocyclic lactone ring inhibitors", ParentId = null },
            new() { Name = "Beta-lactams", Description = "Beta-lactam ring inhibitors", ParentId = null },
            new() { Name = "Aminoglycosides", Description = "Protein synthesis inhibitors", ParentId = null },
        };
        await _context.AntibioticGroups.AddRangeAsync(seeded, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var idByName = seeded.ToDictionary(x => x.Name, x => x.Id);

        var result = await _handler.HandleAsync(new GetAntibioticGroupsQuery(),
            TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        Assert.Equal(
        [
            "Aminoglycosides",
            "Beta-lactams",
            "Macrolides",
        ], [.. result.Data.AntibioticGroups.Select(x => x.Name)]);

        // The projection must keep the ID together with the name
        var betaLactams = Assert.Single(result.Data.AntibioticGroups, x => x.Name == "Beta-lactams");
        Assert.Equal(idByName["Beta-lactams"], betaLactams.Id);
        Assert.All(result.Data.AntibioticGroups, x => Assert.NotEqual(Guid.Empty, x.Id));
    }

    [Fact]
    public async Task GetAntibioticGroups_ExcludesSoftDeletedGroups_Success()
    {
        var alive = new AntibioticGroup
        {
            Name = "Glycopeptides",
            Description = "Cell wall synthesis inhibitors against gram-positive bacteria",
            ParentId = null,
        };
        var softDeleted = new AntibioticGroup
        {
            Name = "Polypeptides",
            Description = "Discontinued classification branch",
            ParentId = null,
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow,
        };
        await _context.AntibioticGroups.AddRangeAsync([alive, softDeleted], TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _handler.HandleAsync(new GetAntibioticGroupsQuery(),
            TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        var item = Assert.Single(result.Data.AntibioticGroups);
        Assert.Equal("Glycopeptides", item.Name);
    }

    /*=== boundary: no data at all ===*/

    [Fact]
    public async Task GetAntibioticGroups_EmptyDatabase_ReturnsEmpty()
    {
        var result = await _handler.HandleAsync(new GetAntibioticGroupsQuery(),
            TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        Assert.Empty(result.Data.AntibioticGroups);
    }

    # endregion
}
