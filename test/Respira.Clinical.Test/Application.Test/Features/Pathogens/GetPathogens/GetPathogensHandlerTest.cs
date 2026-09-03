using Application.Contracts.Data;
using Application.Features.Pathogens.GetPathogens;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.Pathogens.GetPathogens;

public class GetPathogensHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly GetPathogensHandler _handler;
    private readonly IDbContext _context;

    public GetPathogensHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
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
        await _context.Pathogens.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    # region Happy path

    [Fact]
    public async Task GetPathogens_ReturnsAllSortedByNameAscending_Success()
    {
        // Inserted deliberately out of alphabetical order
        var seeded = new List<Pathogen>
        {
            new() { Name = "Pseudomonas aeruginosa", Description = "Gram-negative rod" },
            new() { Name = "Klebsiella pneumoniae", Description = "Gram-negative bacillus" },
            new() { Name = "Staphylococcus aureus", Description = "Gram-positive coccus" },
        };
        await _context.Pathogens.AddRangeAsync(seeded, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var idByName = seeded.ToDictionary(x => x.Name, x => x.Id);

        var result = await _handler.HandleAsync(new GetPathogensQuery(), TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        Assert.Equal(
        [
            "Klebsiella pneumoniae",
            "Pseudomonas aeruginosa",
            "Staphylococcus aureus",
        ], [.. result.Data.Pathogens.Select(x => x.Name)]);

        // The projection must keep the ID together with the name
        var klebsiella = Assert.Single(result.Data.Pathogens, x => x.Name == "Klebsiella pneumoniae");
        Assert.Equal(idByName["Klebsiella pneumoniae"], klebsiella.Id);
        Assert.All(result.Data.Pathogens, x => Assert.NotEqual(Guid.Empty, x.Id));
    }

    [Fact]
    public async Task GetPathogens_ExcludesSoftDeletedPathogens_Success()
    {
        var alive = new Pathogen
        {
            Name = "Haemophilus influenzae",
            Description = "Gram-negative coccobacillus",
        };
        var softDeleted = new Pathogen
        {
            Name = "Bordetella pertussis",
            Description = "Whooping cough agent",
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow,
        };
        await _context.Pathogens.AddRangeAsync([alive, softDeleted], TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _handler.HandleAsync(new GetPathogensQuery(), TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        var item = Assert.Single(result.Data.Pathogens);
        Assert.Equal("Haemophilus influenzae", item.Name);
    }

    /*=== boundary: no data at all ===*/

    [Fact]
    public async Task GetPathogens_EmptyDatabase_ReturnsEmpty()
    {
        var result = await _handler.HandleAsync(new GetPathogensQuery(), TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        Assert.Empty(result.Data.Pathogens);
    }

    # endregion
}
