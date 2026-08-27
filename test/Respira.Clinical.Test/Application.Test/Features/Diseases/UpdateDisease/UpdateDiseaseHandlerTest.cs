using Application.Features.Diseases.UpdateDisease;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Exceptions;

namespace Application.Test.Features.Diseases.UpdateDisease;

public class UpdateDiseaseHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly UpdateDiseaseHandler _handler;
    private readonly AppDbContext _context;

    public UpdateDiseaseHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new UpdateDiseaseMapper();
        var logger = new Mock<ILogger<UpdateDiseaseHandler>>().Object;

        _handler = new(_context, mapper, logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await CleanupDiseasesAsync();
    }

    private async Task CleanupDiseasesAsync()
    {
        var all = await _context.Diseases
            .IgnoreQueryFilters()
            .ToListAsync(TestContext.Current.CancellationToken);
        _context.Diseases.RemoveRange(all);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<Disease> SeedDiseaseAsync(string name, string description, int threshold)
    {
        var disease = new Disease
        {
            Name = name,
            Description = description,
            IcuScoreThreshold = threshold,
        };
        await _context.Diseases.AddAsync(disease, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return disease;
    }

    # region Happy path

    [Fact]
    public async Task UpdateDisease_UpdatesAllFields_Success()
    {
        await CleanupDiseasesAsync();
        var seeded = await SeedDiseaseAsync(
            "Community-Acquired Pneumonia",
            "Infection of the lung parenchyma acquired outside of a healthcare setting",
            3);
        var updatedBefore = seeded.UpdatedAt;

        var command = new UpdateDiseaseCommand
        {
            Id = seeded.Id,
            Name = "Hospital-Acquired Pneumonia",
            Description = "Pneumonia developing more than 48 hours after hospital admission",
            IcuScoreThreshold = 5,
        };

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Verify through a fresh context so the change tracker cannot mask a failed commit
        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.Diseases
            .SingleAsync(x => x.Id == seeded.Id, TestContext.Current.CancellationToken);

        // Business rule: updated scalar fields are persisted
        Assert.Equal("Hospital-Acquired Pneumonia", saved.Name);
        Assert.Equal("Pneumonia developing more than 48 hours after hospital admission", saved.Description);
        Assert.Equal(5, saved.IcuScoreThreshold);

        // Business rule: the primary key is never altered by an update
        Assert.Equal(seeded.Id, saved.Id);

        // Business rule: the update timestamp is refreshed
        Assert.InRange(saved.UpdatedAt.ToUnixTimeMilliseconds(),
            updatedBefore.AddSeconds(-1).ToUnixTimeMilliseconds(),
            DateTimeOffset.UtcNow.AddSeconds(5).ToUnixTimeMilliseconds());
        Assert.True(saved.UpdatedAt >= updatedBefore);
    }

    [Fact]
    public async Task UpdateDisease_BoundaryThresholdOne_Success()
    {
        await CleanupDiseasesAsync();
        var seeded = await SeedDiseaseAsync(
            "Sepsis",
            "Life-threatening organ dysfunction due to dysregulated host response",
            4);

        // Boundary value technique: IcuScoreThreshold of 1 is the smallest accepted value
        var command = new UpdateDiseaseCommand
        {
            Id = seeded.Id,
            Name = "Severe Sepsis",
            Description = "Sepsis with refractory hypotension requiring vasopressors",
            IcuScoreThreshold = 1,
        };

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.Diseases
            .SingleAsync(x => x.Id == seeded.Id, TestContext.Current.CancellationToken);

        Assert.Equal(1, saved.IcuScoreThreshold);
        Assert.Equal("Severe Sepsis", saved.Name);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task UpdateDisease_NotFound_Fail()
    {
        await CleanupDiseasesAsync();
        var unknownId = Guid.CreateVersion7();

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(
            new UpdateDiseaseCommand
            {
                Id = unknownId,
                Name = "Tuberculosis",
                Description = "Infection caused by Mycobacterium tuberculosis",
                IcuScoreThreshold = 4,
            }, TestContext.Current.CancellationToken));

        // Nothing must be created/modified for a missing disease
        Assert.Equal(0, await _context.Diseases
            .CountAsync(TestContext.Current.CancellationToken));
    }

    # endregion
}
