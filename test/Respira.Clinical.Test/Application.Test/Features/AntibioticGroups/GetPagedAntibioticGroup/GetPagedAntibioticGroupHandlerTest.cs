using Application.Contracts.Data;
using Application.Contracts.Mappers;
using Application.Features.AntibioticGroups.GetPagedAntibioticGroup;
using Domain.Models;
using Infrastructure.Data;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.ServiceDefaults.Dtos;

namespace Application.Test.Features.AntibioticGroups.GetPagedAntibioticGroup;

public class GetPagedAntibioticGroupHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly GetPagedAntibioticGroupHandler _handler;
    private readonly IDbContext _context;

    public GetPagedAntibioticGroupHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        IPaginationFactory factory = new PaginationFactory();

        // Initialize handler
        _handler = new(_context, factory);
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

    /*
     * Seeds a beta-lactam hierarchy: one root group and two subgroups whose CreatedAt
     * are spaced 1 minute apart so that the CreatedAt-descending order of the handler
     * is deterministic. Higher suffix means newer
     */
    private async Task<AntibioticGroup> SeedHierarchyAsync()
    {
        var baseTime = DateTimeOffset.UtcNow;

        var root = new AntibioticGroup
        {
            Name = "Beta-lactams",
            Description = "Cell wall synthesis inhibitors sharing the beta-lactam ring",
            ParentId = null,
            CreatedAt = baseTime.AddMinutes(-3),
        };
        var penicillins = new AntibioticGroup
        {
            Name = "Penicillins",
            Description = "Beta-lactam antibiotics active against gram-positive organisms",
            ParentId = root.Id,
            CreatedAt = baseTime.AddMinutes(-1),
        };
        var cephalosporins = new AntibioticGroup
        {
            Name = "Cephalosporins",
            Description = "Beta-lactam antibiotics resistant to staphylococcal beta-lactamase",
            ParentId = root.Id,
            CreatedAt = baseTime.AddMinutes(-2),
        };
        var macrolides = new AntibioticGroup
        {
            Name = "Macrolides",
            Description = "Protein synthesis inhibitors with a macrocyclic lactone ring",
            ParentId = null,
            CreatedAt = baseTime,
        };

        await _context.AntibioticGroups
            .AddRangeAsync([root, cephalosporins, penicillins, macrolides], TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return root;
    }

    # region Happy path

    [Fact]
    public async Task GetPagedAntibioticGroup_NoFilter_FirstPageNewestFirstWithParentName_Success()
    {
        await SeedHierarchyAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibioticGroupQuery
        {
            Param = new PaginationParam { Page = 1, Size = 2 },
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        Assert.Equal(["Macrolides", "Penicillins"], [.. result.Data.Items.Select(x => x.Name)]);

        // Newest item is a root group: ParentName must stay null while the subgroup
        // carries its parent's name through the navigation projection
        Assert.Null(result.Data.Items.Single(x => x.Name == "Macrolides").ParentName);
        var penicillins = result.Data.Items.Single(x => x.Name == "Penicillins");
        Assert.Equal("Beta-lactams", penicillins.ParentName);
        Assert.NotNull(penicillins.ParentId);

        Assert.Equal(1, result.Data.Metadata.CurrentPage);
        Assert.Equal(2, result.Data.Metadata.PageSize);
        Assert.Equal(4, result.Data.Metadata.TotalItemCount);
        Assert.Equal(2, result.Data.Metadata.PageCount);
        Assert.False(result.Data.Metadata.HasPreviousPage);
        Assert.True(result.Data.Metadata.HasNextPage);
    }

    [Fact]
    public async Task GetPagedAntibioticGroup_LastPage_HasNoNext_Success()
    {
        // Upper boundary page: exactly the last two items, no next page
        await SeedHierarchyAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibioticGroupQuery
        {
            Param = new PaginationParam { Page = 2, Size = 2 },
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        Assert.Equal(["Cephalosporins", "Beta-lactams"], [.. result.Data.Items.Select(x => x.Name)]);
        Assert.True(result.Data.Metadata.HasPreviousPage);
        Assert.False(result.Data.Metadata.HasNextPage);
        Assert.Equal(2, result.Data.Metadata.CurrentPage);
    }

    /*=== filter ===*/

    [Fact]
    public async Task GetPagedAntibioticGroup_NameFilter_CaseInsensitivePartialMatch_Success()
    {
        await SeedHierarchyAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibioticGroupQuery
        {
            Param = new PaginationParam { Page = 1, Size = 10 },
            Filter = new AntibioticGroupFilter { Name = "LACTAM" },
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        Assert.Equal(["Beta-lactams"], [.. result.Data.Items.Select(x => x.Name)]);
        Assert.Equal(1, result.Data.Metadata.TotalItemCount);
    }

    [Fact]
    public async Task GetPagedAntibioticGroup_ParentIdFilter_ReturnsDirectChildrenOnly_Success()
    {
        var root = await SeedHierarchyAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibioticGroupQuery
        {
            Param = new PaginationParam { Page = 1, Size = 10 },
            Filter = new AntibioticGroupFilter { ParentId = root.Id },
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        // Newest child first; grandchildren (none here) and other roots excluded
        Assert.Equal(["Penicillins", "Cephalosporins"], [.. result.Data.Items.Select(x => x.Name)]);
        Assert.All(result.Data.Items, x =>
        {
            Assert.Equal(root.Id, x.ParentId);
            Assert.Equal("Beta-lactams", x.ParentName);
        });
    }

    [Fact]
    public async Task GetPagedAntibioticGroup_CombinedFilters_AppliedTogether_Success()
    {
        var root = await SeedHierarchyAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibioticGroupQuery
        {
            Param = new PaginationParam { Page = 1, Size = 10 },
            Filter = new AntibioticGroupFilter { Name = "penic", ParentId = root.Id },
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        var item = Assert.Single(result.Data.Items);
        Assert.Equal("Penicillins", item.Name);
    }

    [Fact]
    public async Task GetPagedAntibioticGroup_FilterMatchesNothing_ReturnsEmpty()
    {
        await SeedHierarchyAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibioticGroupQuery
        {
            Param = new PaginationParam { Page = 1, Size = 10 },
            Filter = new AntibioticGroupFilter { Name = "Tetracyclines" },
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        Assert.Empty(result.Data.Items);
        Assert.Equal(0, result.Data.Metadata.TotalItemCount);
    }

    # endregion
}
