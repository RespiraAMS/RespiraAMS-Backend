using Application.Features.Diseases.GetPagedDisease;
using Domain.Models;
using Infrastructure.Data;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Dtos;

namespace Application.Test.Features.Diseases.GetPagedDisease;

public class GetPagedDiseaseHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly GetPagedDiseaseHandler _handler;
    private readonly AppDbContext _context;

    public GetPagedDiseaseHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var factory = new PaginationFactory();

        _handler = new(_context, factory);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await CleanupAsync();
    }

    private async Task CleanupAsync()
    {
        _context.Diseases.RemoveRange(
            await _context.Diseases.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<Disease> SeedDiseaseAsync(string name, DateTimeOffset? createdAt = null)
    {
        var disease = new Disease
        {
            Name = name,
            Description = $"Clinical description for {name}",
            IcuScoreThreshold = 3,
        };
        if (createdAt is not null)
        {
            disease.CreatedAt = createdAt.Value;
        }

        _context.Diseases.Add(disease);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return disease;
    }

    # region Happy path

    [Fact]
    public async Task GetPagedDisease_ReturnsAllDiseases_Success()
    {
        await CleanupAsync();
        var a = await SeedDiseaseAsync("Community-Acquired Pneumonia");
        var b = await SeedDiseaseAsync("Hospital-Acquired Pneumonia");
        var c = await SeedDiseaseAsync("Tuberculosis");

        var result = await _handler.HandleAsync(
            new GetPagedDiseaseQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
            },
            TestContext.Current.CancellationToken);

        // Business rule: all non-deleted diseases are returned
        Assert.Equal(3, result.Metadata.TotalItemCount);
        Assert.Equal(3, result.Items.Count());
        Assert.Equal(1, result.Metadata.PageCount);
        Assert.Equal(1, result.Metadata.CurrentPage);
        Assert.Equal(10, result.Metadata.PageSize);
        Assert.False(result.Metadata.HasNextPage);
        Assert.False(result.Metadata.HasPreviousPage);

        // Business rule: each item projects Id and Name
        var ids = result.Items.Select(x => x.Id).ToHashSet();
        Assert.Contains(a.Id, ids);
        Assert.Contains(b.Id, ids);
        Assert.Contains(c.Id, ids);
        Assert.All(result.Items, item =>
        {
            Assert.NotEqual(Guid.Empty, item.Id);
            Assert.False(string.IsNullOrEmpty(item.Name));
        });
    }

    [Fact]
    public async Task GetPagedDisease_FilterByName_ILikeContains_Success()
    {
        await CleanupAsync();
        await SeedDiseaseAsync("Community-Acquired Pneumonia");
        await SeedDiseaseAsync("Hospital-Acquired Pneumonia");
        await SeedDiseaseAsync("Tuberculosis");

        // Business rule: the Name filter performs a case-insensitive substring (ILike) match
        var result = await _handler.HandleAsync(
            new GetPagedDiseaseQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new DiseaseFilter { Name = "pneumonia" },
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Metadata.TotalItemCount);
        Assert.Equal(2, result.Items.Count());
        Assert.All(result.Items, item => Assert.Contains("Pneumonia", item.Name, StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetPagedDisease_NoFilter_ReturnsEverything_Success()
    {
        await CleanupAsync();
        await SeedDiseaseAsync("Community-Acquired Pneumonia");
        await SeedDiseaseAsync("Tuberculosis");

        // Business rule: when Filter is omitted, no name filter is applied
        var result = await _handler.HandleAsync(
            new GetPagedDiseaseQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = null,
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Metadata.TotalItemCount);
    }

    // Boundary value technique: Size = 1 is the smallest valid page size
    [Fact]
    public async Task GetPagedDisease_PagingSizeBoundaryMin_Success()
    {
        await CleanupAsync();
        for (var i = 0; i < 5; i++)
        {
            await SeedDiseaseAsync($"Disease {i}");
        }

        var result = await _handler.HandleAsync(
            new GetPagedDiseaseQuery
            {
                Param = new PaginationParam { Page = 1, Size = 1 },
            },
            TestContext.Current.CancellationToken);

        Assert.Single(result.Items);
        Assert.Equal(1, result.Metadata.PageSize);
        Assert.Equal(5, result.Metadata.TotalItemCount);
        Assert.Equal(5, result.Metadata.PageCount);
        Assert.True(result.Metadata.HasNextPage);
        Assert.False(result.Metadata.HasPreviousPage);
    }

    [Fact]
    public async Task GetPagedDisease_SecondPage_ReturnsRemaining_Success()
    {
        await CleanupAsync();
        for (var i = 0; i < 5; i++)
        {
            await SeedDiseaseAsync($"Disease {i}");
        }

        var result = await _handler.HandleAsync(
            new GetPagedDiseaseQuery
            {
                Param = new PaginationParam { Page = 2, Size = 2 },
            },
            TestContext.Current.CancellationToken);

        // ceil(5 / 2) = 3 pages; page 2 holds items 3 and 4
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(5, result.Metadata.TotalItemCount);
        Assert.Equal(3, result.Metadata.PageCount);
        Assert.Equal(2, result.Metadata.CurrentPage);
        Assert.True(result.Metadata.HasNextPage);
        Assert.True(result.Metadata.HasPreviousPage);
    }

    [Fact]
    public async Task GetPagedDisease_PageBeyondTotal_ReturnsEmpty_Success()
    {
        await CleanupAsync();
        await SeedDiseaseAsync("Community-Acquired Pneumonia");
        await SeedDiseaseAsync("Tuberculosis");

        var result = await _handler.HandleAsync(
            new GetPagedDiseaseQuery
            {
                Param = new PaginationParam { Page = 5, Size = 10 },
            },
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Items);
        Assert.Equal(2, result.Metadata.TotalItemCount);
        Assert.Equal(1, result.Metadata.PageCount);
        Assert.False(result.Metadata.HasNextPage);
        // X.PagedList clamps the reported current page to the last page, so the
        // beyond-range request yields an empty page without throwing.
        Assert.False(result.Metadata.HasPreviousPage);
    }

    [Fact]
    public async Task GetPagedDisease_OrdersByCreatedAtDescending_Success()
    {
        await CleanupAsync();
        var baseTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await SeedDiseaseAsync("Oldest Disease", baseTime);
        await SeedDiseaseAsync("Middle Disease", baseTime.AddMinutes(1));
        await SeedDiseaseAsync("Newest Disease", baseTime.AddMinutes(2));

        // Business rule: results are ordered by CreatedAt descending (newest first)
        var result = await _handler.HandleAsync(
            new GetPagedDiseaseQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
            },
            TestContext.Current.CancellationToken);

        var orderedNames = result.Items.Select(x => x.Name).ToList();
        Assert.Equal(new[] { "Newest Disease", "Middle Disease", "Oldest Disease" }, orderedNames);
    }

    [Fact]
    public async Task GetPagedDisease_ExcludesSoftDeleted_Success()
    {
        await CleanupAsync();
        var active = await SeedDiseaseAsync("Active Disease");
        var deleted = await SeedDiseaseAsync("Deleted Disease");
        deleted.IsDeleted = true;
        deleted.DeletedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Business rule: soft-deleted diseases are hidden by the global query filter
        var result = await _handler.HandleAsync(
            new GetPagedDiseaseQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(1, result.Metadata.TotalItemCount);
        var single = Assert.Single(result.Items);
        Assert.Equal(active.Id, single.Id);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task GetPagedDisease_NoDiseases_ReturnsEmpty_Success()
    {
        await CleanupAsync();

        // Business rule: a query with no matching diseases yields an empty page (not an error)
        var result = await _handler.HandleAsync(
            new GetPagedDiseaseQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
            },
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Metadata.TotalItemCount);
    }

    # endregion
}
