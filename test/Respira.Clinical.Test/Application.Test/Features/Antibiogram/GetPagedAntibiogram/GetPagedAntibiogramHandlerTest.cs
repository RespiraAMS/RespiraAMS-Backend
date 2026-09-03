using Application.Contracts.Data;
using Application.Contracts.Mappers;
using Application.Features.Antibiograms.GetPagedAntibiogram;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.ServiceDefaults.Dtos;

namespace Application.Test.Features.Antibiogram.GetPagedAntibiogram;

using Antibiogram = Domain.Models.Antibiogram;

public class GetPagedAntibiogramHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly GetPagedAntibiogramHandler _handler;
    private readonly IDbContext _context;

    public GetPagedAntibiogramHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
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
        // The antibiogram-antibiotic join tables are not exposed as DbSets, so clear
        // them with raw SQL before deleting the entities that reference them.
        // IgnoreQueryFilters is needed because soft-deleted rows are hidden by the
        // query filter but still occupy the table
        var db = (AppDbContext)_context;
        await db.Database.ExecuteSqlRawAsync("DELETE FROM antibiogram_mic_groups",
            TestContext.Current.CancellationToken);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM antibiogram_first_priority_medicines",
            TestContext.Current.CancellationToken);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM antibiogram_second_priority_medicines",
            TestContext.Current.CancellationToken);
        await _context.Antibiograms.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Dosages.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Antibiotics.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Pathogens.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.AntibioticGroups.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    /*
     * Seeds one group, two pathogens, three antibiotics and four antibiograms whose
     * CreatedAt are spaced 1 minute apart so the CreatedAt-descending order of the
     * handler is deterministic. The oldest one is soft-deleted to prove the query
     * filter hides it. Newest active first: Kp-Resistance (P2), Kp-Susceptible (P1),
     * Kp-Intermediate (P1); the soft-deleted Kp-Deleted (P1) must never surface
     */
    private async Task<(Guid PneumoniaeId, Guid AeruginosaId)> SeedAsync()
    {
        var baseTime = DateTimeOffset.UtcNow;

        var group = new AntibioticGroup
        {
            Name = "Beta-lactams",
            Description = "Cell wall synthesis inhibitors sharing the beta-lactam ring",
            ParentId = null,
        };
        var pneumoniae = new Pathogen
        {
            Name = "Klebsiella pneumoniae",
            Description = "Gram-negative bacillus",
        };
        var aeruginosa = new Pathogen
        {
            Name = "Pseudomonas aeruginosa",
            Description = "Gram-negative rod",
        };
        var amoxicillin = new Antibiotic
        {
            Name = "Amoxicillin",
            AntibioticGroupId = group.Id,
            Classification = AwareClassification.Access,
        };
        var meropenem = new Antibiotic
        {
            Name = "Meropenem",
            AntibioticGroupId = group.Id,
            Classification = AwareClassification.Watch,
        };
        var ciprofloxacin = new Antibiotic
        {
            Name = "Ciprofloxacin",
            AntibioticGroupId = group.Id,
            Classification = AwareClassification.Watch,
        };

        var intermediate = new Antibiogram
        {
            PathogenId = pneumoniae.Id,
            MicLevel = MinimumInhibitoryConcentration.Intermediate,
            Mics = [amoxicillin],
            FirstPriorityMedicines = [meropenem],
            SecondPriorityMedicines = [ciprofloxacin],
            CreatedAt = baseTime,
        };
        var susceptible = new Antibiogram
        {
            PathogenId = pneumoniae.Id,
            MicLevel = MinimumInhibitoryConcentration.Susceptible,
            Mics = [meropenem],
            FirstPriorityMedicines = [amoxicillin],
            SecondPriorityMedicines = [ciprofloxacin],
            CreatedAt = baseTime.AddMinutes(1),
        };
        var resistance = new Antibiogram
        {
            PathogenId = aeruginosa.Id,
            MicLevel = MinimumInhibitoryConcentration.Resistance,
            Mics = [ciprofloxacin],
            FirstPriorityMedicines = [meropenem],
            SecondPriorityMedicines = [amoxicillin],
            CreatedAt = baseTime.AddMinutes(2),
        };
        var softDeleted = new Antibiogram
        {
            PathogenId = pneumoniae.Id,
            MicLevel = MinimumInhibitoryConcentration.Resistance,
            Mics = [amoxicillin],
            FirstPriorityMedicines = [amoxicillin],
            SecondPriorityMedicines = [amoxicillin],
            CreatedAt = baseTime.AddMinutes(3),
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow,
        };

        await _context.AntibioticGroups.AddAsync(group, TestContext.Current.CancellationToken);
        await _context.Pathogens.AddRangeAsync([pneumoniae, aeruginosa], TestContext.Current.CancellationToken);
        await _context.Antibiotics.AddRangeAsync([amoxicillin, meropenem, ciprofloxacin],
            TestContext.Current.CancellationToken);
        await _context.Antibiograms.AddRangeAsync(
            [intermediate, susceptible, resistance, softDeleted], TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (pneumoniae.Id, aeruginosa.Id);
    }

    # region Happy path

    [Fact]
    public async Task GetPagedAntibiogram_NoFilter_FirstPageNewestFirstWithNestedData_Success()
    {
        var (_, aeruginosaId) = await SeedAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibiogramQuery
        {
            Param = new PaginationParam { Page = 1, Size = 2 },
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Equal(Status.Success, result.StatusCode);

        // Newest active first; the soft-deleted antibiogram must stay hidden
        Assert.Equal(
        [
            MinimumInhibitoryConcentration.Resistance,
            MinimumInhibitoryConcentration.Susceptible,
        ], [.. result.Data.Items.Select(x => x.MicLevel)]);

        // The nested projections must carry names and IDs through the navigations
        var newest = result.Data.Items.First();
        Assert.Equal(aeruginosaId, newest.Pathogen.Id);
        Assert.Equal("Pseudomonas aeruginosa", newest.Pathogen.Name);
        Assert.Equal(["Ciprofloxacin"], [.. newest.Mics.Select(x => x.Name)]);
        Assert.Equal(["Meropenem"], [.. newest.FirstPriorityMedicines.Select(x => x.Name)]);
        Assert.Equal(["Amoxicillin"], [.. newest.SecondPriorityMedicines.Select(x => x.Name)]);

        var second = result.Data.Items.Skip(1).First();
        Assert.Equal("Klebsiella pneumoniae", second.Pathogen.Name);
        Assert.Equal(["Meropenem"], [.. second.Mics.Select(x => x.Name)]);

        Assert.Equal(1, result.Data.Metadata.CurrentPage);
        Assert.Equal(2, result.Data.Metadata.PageSize);
        Assert.Equal(3, result.Data.Metadata.TotalItemCount);
        Assert.Equal(2, result.Data.Metadata.PageCount);
        Assert.False(result.Data.Metadata.HasPreviousPage);
        Assert.True(result.Data.Metadata.HasNextPage);
    }

    [Fact]
    public async Task GetPagedAntibiogram_LastPartialPage_HasNoNext_Success()
    {
        // Upper boundary page: only 1 leftover item and no next page
        await SeedAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibiogramQuery
        {
            Param = new PaginationParam { Page = 2, Size = 2 },
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Equal(Status.Success, result.StatusCode);

        var item = Assert.Single(result.Data.Items);
        Assert.Equal(MinimumInhibitoryConcentration.Intermediate, item.MicLevel);
        Assert.True(result.Data.Metadata.HasPreviousPage);
        Assert.False(result.Data.Metadata.HasNextPage);
        Assert.Equal(2, result.Data.Metadata.CurrentPage);
    }

    [Fact]
    public async Task GetPagedAntibiogram_SizeLowerBoundary_SingleItemPerPage_Success()
    {
        // Boundary: the smallest allowed size still paginates correctly
        await SeedAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibiogramQuery
        {
            Param = new PaginationParam { Page = 1, Size = 1 },
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Equal(Status.Success, result.StatusCode);

        _ = Assert.Single(result.Data.Items);
        Assert.Equal(MinimumInhibitoryConcentration.Resistance, result.Data.Items.First().MicLevel);
        Assert.Equal(3, result.Data.Metadata.PageCount);
        Assert.True(result.Data.Metadata.HasNextPage);
    }

    /*=== filter ===*/

    [Fact]
    public async Task GetPagedAntibiogram_PathogenFilter_ReturnsOnlyThatPathogen_Success()
    {
        var (pneumoniaeId, _) = await SeedAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibiogramQuery
        {
            Param = new PaginationParam { Page = 1, Size = 10 },
            Filter = new AntibiogramFilter { PathogenId = pneumoniaeId },
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Equal(Status.Success, result.StatusCode);

        // Susceptible then Intermediate; the soft-deleted pneumoniae row stays hidden
        Assert.Equal(
        [
            MinimumInhibitoryConcentration.Susceptible,
            MinimumInhibitoryConcentration.Intermediate,
        ], [.. result.Data.Items.Select(x => x.MicLevel)]);
        Assert.All(result.Data.Items, x => Assert.Equal(pneumoniaeId, x.Pathogen.Id));
        Assert.Equal(2, result.Data.Metadata.TotalItemCount);
    }

    [Fact]
    public async Task GetPagedAntibiogram_FilterWithNullPathogenId_AppliesNoFiltering_Success()
    {
        // Boundary: a filter object whose PathogenId is null must behave as no filter
        await SeedAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibiogramQuery
        {
            Param = new PaginationParam { Page = 1, Size = 10 },
            Filter = new AntibiogramFilter { PathogenId = null },
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Equal(Status.Success, result.StatusCode);

        Assert.Equal(3, result.Data.Items.Count());
        Assert.Equal(3, result.Data.Metadata.TotalItemCount);
    }

    [Fact]
    public async Task GetPagedAntibiogram_FilterMatchesNothing_ReturnsEmpty()
    {
        await SeedAsync();

        var result = await _handler.HandleAsync(new GetPagedAntibiogramQuery
        {
            Param = new PaginationParam { Page = 1, Size = 10 },
            Filter = new AntibiogramFilter { PathogenId = Guid.CreateVersion7() },
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Equal(Status.Success, result.StatusCode);

        Assert.Empty(result.Data.Items);
        Assert.Equal(0, result.Data.Metadata.TotalItemCount);
    }

    # endregion
}
