using Application.Contracts.Data;
using Application.Contracts.Mappers;
using Application.Features.Antibiotics.GetPagedAntibiotic;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Dtos;

namespace Application.Test.Features.Antibiotics.GetPagedAntibiotic;

public class GetPagedAntibioticHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly GetPagedAntibioticHandler _handler;
    private readonly IDbContext _context;

    public GetPagedAntibioticHandlerTest(PostgresFixture fixture)
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
     * Seeds two beta-lactams and one macrolide whose CreatedAt are spaced 1 minute
     * apart so that the CreatedAt-descending order of the handler is deterministic.
     * Newest first: Azithromycin, Meropenem, Amoxicillin
     */
    private async Task<(AntibioticGroup BetaLactams, AntibioticGroup Macrolides)> SeedAsync()
    {
        var baseTime = DateTimeOffset.UtcNow;

        var betaLactams = new AntibioticGroup
        {
            Name = "Beta-lactams",
            Description = "Cell wall synthesis inhibitors sharing the beta-lactam ring",
            ParentId = null,
            CreatedAt = baseTime.AddMinutes(-2),
        };
        var macrolides = new AntibioticGroup
        {
            Name = "Macrolides",
            Description = "Protein synthesis inhibitors with a macrocyclic lactone ring",
            ParentId = null,
            CreatedAt = baseTime.AddMinutes(-1),
        };

        var amoxicillin = new Antibiotic
        {
            Name = "Amoxicillin",
            AntibioticGroupId = betaLactams.Id,
            Classification = AwareClassification.Access,
            CreatedAt = baseTime,
        };
        var meropenem = new Antibiotic
        {
            Name = "Meropenem",
            AntibioticGroupId = betaLactams.Id,
            Classification = AwareClassification.Watch,
            CreatedAt = baseTime.AddMinutes(1),
        };
        var azithromycin = new Antibiotic
        {
            Name = "Azithromycin",
            AntibioticGroupId = macrolides.Id,
            Classification = AwareClassification.Watch,
            CreatedAt = baseTime.AddMinutes(2),
        };

        await _context.AntibioticGroups.AddRangeAsync([betaLactams, macrolides],
            TestContext.Current.CancellationToken);
        await _context.Antibiotics.AddRangeAsync([amoxicillin, meropenem, azithromycin],
            TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (betaLactams, macrolides);
    }

    # region Happy path

    [Fact]
    public async Task GetPagedAntibiotic_NoFilter_FirstPageNewestFirstWithGroupName_Success()
    {
        var (betaLactams, _) = await SeedAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibioticQuery
        {
            Param = new PaginationParam { Page = 1, Size = 2 },
        }, TestContext.Current.CancellationToken);

        Assert.Equal(["Azithromycin", "Meropenem"], [.. result.Items.Select(x => x.Name)]);

        // The nested group projection must carry the group name through the navigation
        Assert.Equal("Macrolides", result.Items.Single(x => x.Name == "Azithromycin")
            .AntibioticGroup.Name);
        Assert.Equal(betaLactams.Id, result.Items.Single(x => x.Name == "Meropenem")
            .AntibioticGroup.Id);

        Assert.Equal(1, result.Metadata.CurrentPage);
        Assert.Equal(2, result.Metadata.PageSize);
        Assert.Equal(3, result.Metadata.TotalItemCount);
        Assert.Equal(2, result.Metadata.PageCount);
        Assert.False(result.Metadata.HasPreviousPage);
        Assert.True(result.Metadata.HasNextPage);
    }

    [Fact]
    public async Task GetPagedAntibiotic_LastPartialPage_HasNoNext_Success()
    {
        // Upper boundary page: only 1 leftover item and no next page
        await SeedAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibioticQuery
        {
            Param = new PaginationParam { Page = 2, Size = 2 },
        }, TestContext.Current.CancellationToken);

        var item = Assert.Single(result.Items);
        Assert.Equal("Amoxicillin", item.Name);
        Assert.True(result.Metadata.HasPreviousPage);
        Assert.False(result.Metadata.HasNextPage);
        Assert.Equal(2, result.Metadata.CurrentPage);
    }

    /*=== filter ===*/

    [Fact]
    public async Task GetPagedAntibiotic_NameFilter_CaseInsensitivePartialMatch_Success()
    {
        await SeedAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibioticQuery
        {
            Param = new PaginationParam { Page = 1, Size = 10 },
            Filter = new AntibioticFilter { Name = "MYCIN" },
        }, TestContext.Current.CancellationToken);

        // Uppercase pattern against stored lowercase "mycin" proves ILike
        // case-insensitivity; Amoxicillin does not contain the fragment
        var item = Assert.Single(result.Items);
        Assert.Equal("Azithromycin", item.Name);
        Assert.Equal(1, result.Metadata.TotalItemCount);
    }

    [Fact]
    public async Task GetPagedAntibiotic_GroupFilter_ReturnsOnlyThatGroup_Success()
    {
        var (_, macrolides) = await SeedAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibioticQuery
        {
            Param = new PaginationParam { Page = 1, Size = 10 },
            Filter = new AntibioticFilter { AntibioticGroupId = macrolides.Id },
        }, TestContext.Current.CancellationToken);

        var item = Assert.Single(result.Items);
        Assert.Equal("Azithromycin", item.Name);
        Assert.Equal(macrolides.Id, item.AntibioticGroup.Id);
    }

    [Fact]
    public async Task GetPagedAntibiotic_ClassificationFilter_ReturnsOnlyMatchingOnes_Success()
    {
        await SeedAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibioticQuery
        {
            Param = new PaginationParam { Page = 1, Size = 10 },
            Filter = new AntibioticFilter { Classification = AwareClassification.Watch },
        }, TestContext.Current.CancellationToken);

        Assert.Equal(
        [
            "Azithromycin",
            "Meropenem",
        ], [.. result.Items.Select(x => x.Name)]);
    }

    [Fact]
    public async Task GetPagedAntibiotic_CombinedFilters_AppliedTogether_Success()
    {
        var (betaLactams, _) = await SeedAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibioticQuery
        {
            Param = new PaginationParam { Page = 1, Size = 10 },
            Filter = new AntibioticFilter
            {
                AntibioticGroupId = betaLactams.Id,
                Classification = AwareClassification.Watch,
            },
        }, TestContext.Current.CancellationToken);

        var item = Assert.Single(result.Items);
        Assert.Equal("Meropenem", item.Name);
    }

    [Fact]
    public async Task GetPagedAntibiotic_FilterMatchesNothing_ReturnsEmpty()
    {
        await SeedAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibioticQuery
        {
            Param = new PaginationParam { Page = 1, Size = 10 },
            Filter = new AntibioticFilter { Name = "Vancomycin" },
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Metadata.TotalItemCount);
    }

    # endregion
}
