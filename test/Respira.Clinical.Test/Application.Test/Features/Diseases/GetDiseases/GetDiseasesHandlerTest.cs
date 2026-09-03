using Application.Features.Diseases.GetDiseases;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.Diseases.GetDiseases;

public class GetDiseasesHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly GetDiseasesHandler _handler;
    private readonly AppDbContext _context;

    public GetDiseasesHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);

        _handler = new(_context);
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

    private async Task<Disease> SeedDiseaseAsync(string name, bool softDeleted = false)
    {
        var disease = new Disease
        {
            Name = name,
            Description = $"Clinical description for {name}",
            IcuScoreThreshold = 3,
        };
        if (softDeleted)
        {
            disease.IsDeleted = true;
            disease.DeletedAt = DateTimeOffset.UtcNow;
        }

        _context.Diseases.Add(disease);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return disease;
    }

    # region Happy path

    [Fact]
    public async Task GetDiseases_ReturnsAllDiseases_Success()
    {
        await CleanupAsync();
        var a = await SeedDiseaseAsync("Community-Acquired Pneumonia");
        var b = await SeedDiseaseAsync("Hospital-Acquired Pneumonia");
        var c = await SeedDiseaseAsync("Tuberculosis");

        var result = await _handler.HandleAsync(
            new GetDiseasesQuery(),
            TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        // Business rule: every non-deleted disease is projected with its Id and Name
        Assert.Equal(3, result.Data.Diseases.Count());
        var ids = result.Data.Diseases.Select(x => x.Id).ToHashSet();
        Assert.Contains(a.Id, ids);
        Assert.Contains(b.Id, ids);
        Assert.Contains(c.Id, ids);
        Assert.All(result.Data.Diseases, item =>
        {
            Assert.NotEqual(Guid.Empty, item.Id);
            Assert.False(string.IsNullOrEmpty(item.Name));
        });
    }

    [Fact]
    public async Task GetDiseases_OrdersByNameAscending_Success()
    {
        await CleanupAsync();
        await SeedDiseaseAsync("Tuberculosis");
        await SeedDiseaseAsync("Anthrax");
        await SeedDiseaseAsync("Influenza");

        var result = await _handler.HandleAsync(
            new GetDiseasesQuery(),
            TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        // Business rule: diseases are ordered alphabetically by name (ascending)
        var orderedNames = result.Data.Diseases.Select(x => x.Name).ToList();
        Assert.Equal(new[] { "Anthrax", "Influenza", "Tuberculosis" }, orderedNames);
    }

    [Fact]
    public async Task GetDiseases_ExcludesSoftDeleted_Success()
    {
        await CleanupAsync();
        var active = await SeedDiseaseAsync("Active Disease");
        await SeedDiseaseAsync("Deleted Disease", softDeleted: true);

        var result = await _handler.HandleAsync(
            new GetDiseasesQuery(),
            TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        // Business rule: soft-deleted diseases are hidden by the global query filter
        Assert.Single(result.Data.Diseases);
        var single = Assert.Single(result.Data.Diseases);
        Assert.Equal(active.Id, single.Id);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task GetDiseases_NoDiseases_ReturnsEmpty_Success()
    {
        await CleanupAsync();

        // Business rule: with no diseases the handler returns an empty list, not an error
        var result = await _handler.HandleAsync(
            new GetDiseasesQuery(),
            TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        Assert.Empty(result.Data.Diseases);
    }

    # endregion
}
