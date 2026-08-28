using Application.Contracts.Data;
using Application.Features.Antibiotics.GetAntibiotics;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Test.Features.Antibiotics.GetAntibiotics;

public class GetAntibioticsHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly GetAntibioticsHandler _handler;
    private readonly IDbContext _context;

    public GetAntibioticsHandlerTest(PostgresFixture fixture)
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

    # region Happy path

    [Fact]
    public async Task GetAntibiotics_ReturnsAllSortedByNameAscending_Success()
    {
        // Inserted deliberately out of alphabetical order
        var group = new AntibioticGroup
        {
            Name = "Beta-lactams",
            Description = "Cell wall synthesis inhibitors sharing the beta-lactam ring",
            ParentId = null,
        };
        var seeded = new List<Antibiotic>
        {
            new() { Name = "Meropenem", AntibioticGroupId = group.Id,
                Classification = Domain.Enums.AwareClassification.Watch },
            new() { Name = "Amoxicillin", AntibioticGroupId = group.Id,
                Classification = Domain.Enums.AwareClassification.Access },
            new() { Name = "Co-amoxiclav", AntibioticGroupId = group.Id,
                Classification = Domain.Enums.AwareClassification.AccessWatch },
        };
        await _context.AntibioticGroups.AddAsync(group, TestContext.Current.CancellationToken);
        await _context.Antibiotics.AddRangeAsync(seeded, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var idByName = seeded.ToDictionary(x => x.Name, x => x.Id);

        var result = await _handler.HandleAsync(new GetAntibioticsQuery(),
            TestContext.Current.CancellationToken);

        Assert.Equal(
        [
            "Amoxicillin",
            "Co-amoxiclav",
            "Meropenem",
        ], result.Antibiotics.Select(x => x.Name).ToArray());

        // The projection must keep the ID together with the name
        var amoxicillin = Assert.Single(result.Antibiotics, x => x.Name == "Amoxicillin");
        Assert.Equal(idByName["Amoxicillin"], amoxicillin.Id);
        Assert.All(result.Antibiotics, x => Assert.NotEqual(Guid.Empty, x.Id));
    }

    [Fact]
    public async Task GetAntibiotics_ExcludesSoftDeletedAntibiotics_Success()
    {
        var group = new AntibioticGroup
        {
            Name = "Macrolides",
            Description = "Protein synthesis inhibitors with a macrocyclic lactone ring",
            ParentId = null,
        };
        var alive = new Antibiotic
        {
            Name = "Azithromycin",
            AntibioticGroupId = group.Id,
            Classification = Domain.Enums.AwareClassification.Watch,
        };
        var softDeleted = new Antibiotic
        {
            Name = "Telithromycin",
            AntibioticGroupId = group.Id,
            Classification = Domain.Enums.AwareClassification.Others,
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow,
        };
        await _context.AntibioticGroups.AddAsync(group, TestContext.Current.CancellationToken);
        await _context.Antibiotics.AddRangeAsync([alive, softDeleted], TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _handler.HandleAsync(new GetAntibioticsQuery(),
            TestContext.Current.CancellationToken);

        var item = Assert.Single(result.Antibiotics);
        Assert.Equal("Azithromycin", item.Name);
    }

    /*=== boundary: no data at all ===*/

    [Fact]
    public async Task GetAntibiotics_EmptyDatabase_ReturnsEmpty()
    {
        var result = await _handler.HandleAsync(new GetAntibioticsQuery(),
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Antibiotics);
    }

    # endregion
}
