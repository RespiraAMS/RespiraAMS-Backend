using Application.Contracts.Data;
using Application.Features.Antibiotics.UpdateAntibiotic;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.Antibiotics.UpdateAntibiotic;

public class UpdateAntibioticHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly UpdateAntibioticHandler _handler;
    private readonly IDbContext _context;

    public UpdateAntibioticHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new UpdateAntibioticMapper();
        var logger = new Mock<ILogger<UpdateAntibioticHandler>>().Object;

        // Initialize handler
        _handler = new(_context, mapper, logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        // Antibiotics reference groups through an FK, so delete them first.
        // IgnoreQueryFilters is needed because soft-deleted rows are hidden by the
        // query filter but still occupy the table
        await _context.Dosages.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Antibiotics.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.AntibioticGroups.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    private async Task<AntibioticGroup> SeedGroupAsync(string name, string description,
        bool softDeleted = false)
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

    private async Task<Antibiotic> SeedAntibioticAsync(string name, AntibioticGroup group,
        AwareClassification classification)
    {
        var antibiotic = new Antibiotic
        {
            Name = name,
            AntibioticGroupId = group.Id,
            Classification = classification,
        };
        antibiotic.DosageIds.Add(Guid.CreateVersion7());
        await _context.Antibiotics.AddAsync(antibiotic, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return antibiotic;
    }

    # region Happy path

    [Theory]
    [InlineData("Co-amoxiclav", "AccessWatch")]
    [InlineData("Piperacillin-tazobactam", "Watch")]
    public async Task UpdateAntibiotic_Success(string newName, string newClassification)
    {
        var group = await SeedGroupAsync("Beta-lactams",
            "Cell wall synthesis inhibitors sharing the beta-lactam ring");
        var seeded = await SeedAntibioticAsync("Amoxicillin", group, AwareClassification.Access);
        var updatedBefore = DateTimeOffset.UtcNow;

        var result = await _handler.HandleAsync(new UpdateAntibioticCommand
        {
            Id = seeded.Id,
            Name = newName,
            AntibioticGroupId = group.Id,
            Classification = Enum.Parse<AwareClassification>(newClassification),
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Updated, result.StatusCode);

        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.Antibiotics.SingleAsync(
            x => x.Id == seeded.Id, TestContext.Current.CancellationToken);
        Assert.Equal(newName, saved.Name);
        Assert.Equal(group.Id, saved.AntibioticGroupId);
        Assert.Equal(Enum.Parse<AwareClassification>(newClassification), saved.Classification);
        Assert.InRange(saved.UpdatedAt.ToUnixTimeMilliseconds(),
            updatedBefore.AddSeconds(-5).ToUnixTimeMilliseconds(),
            DateTimeOffset.UtcNow.AddSeconds(5).ToUnixTimeMilliseconds());
    }

    [Fact]
    public async Task UpdateAntibiotic_MoveToAnotherExistingGroup_Success()
    {
        var oldGroup = await SeedGroupAsync("Beta-lactams",
            "Cell wall synthesis inhibitors sharing the beta-lactam ring");
        var newGroup = await SeedGroupAsync("Macrolides",
            "Protein synthesis inhibitors with a macrocyclic lactone ring");
        var seeded = await SeedAntibioticAsync("Azithromycin", oldGroup, AwareClassification.Watch);

        var result = await _handler.HandleAsync(new UpdateAntibioticCommand
        {
            Id = seeded.Id,
            Name = "Azithromycin",
            AntibioticGroupId = newGroup.Id,
            Classification = AwareClassification.Watch,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Updated, result.StatusCode);

        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.Antibiotics.SingleAsync(
            x => x.Id == seeded.Id, TestContext.Current.CancellationToken);
        Assert.Equal(newGroup.Id, saved.AntibioticGroupId);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task UpdateAntibiotic_AntibioticNotFound_Fail()
    {
        var group = await SeedGroupAsync("Beta-lactams", "Existing group");
        var unknownId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new UpdateAntibioticCommand
            {
                Id = unknownId,
                Name = "Ciprofloxacin",
                AntibioticGroupId = group.Id,
                Classification = AwareClassification.Watch,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // Nothing must be created when the target does not exist
        Assert.Equal(0, await _context.Antibiotics.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateAntibiotic_UnknownGroup_Fail()
    {
        var group = await SeedGroupAsync("Beta-lactams", "Existing group");
        var seeded = await SeedAntibioticAsync("Amoxicillin", group, AwareClassification.Access);
        var unknownGroupId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new UpdateAntibioticCommand
            {
                Id = seeded.Id,
                Name = "Amoxicillin",
                AntibioticGroupId = unknownGroupId,
                Classification = AwareClassification.Access,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // The target must stay untouched when the group does not exist
        await using var freshContext = new AppDbContext(_options);
        var untouched = await freshContext.Antibiotics.SingleAsync(
            x => x.Id == seeded.Id, TestContext.Current.CancellationToken);
        Assert.Equal(group.Id, untouched.AntibioticGroupId);
        Assert.Equal("Amoxicillin", untouched.Name);
    }

    [Fact]
    public async Task UpdateAntibiotic_SoftDeletedGroup_Fail()
    {
        // A soft-deleted group is hidden by the query filter, so referencing it
        // must be rejected just like an unknown group
        var deletedGroup = await SeedGroupAsync("Polypeptides",
            "Discontinued classification branch", softDeleted: true);
        var otherGroup = await SeedGroupAsync("Beta-lactams", "Existing group");
        var seeded = await SeedAntibioticAsync("Colistin", otherGroup, AwareClassification.Reserve);

        var result = await _handler.HandleAsync(
            new UpdateAntibioticCommand
            {
                Id = seeded.Id,
                Name = "Colistin",
                AntibioticGroupId = deletedGroup.Id,
                Classification = AwareClassification.Reserve,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    # endregion
}
