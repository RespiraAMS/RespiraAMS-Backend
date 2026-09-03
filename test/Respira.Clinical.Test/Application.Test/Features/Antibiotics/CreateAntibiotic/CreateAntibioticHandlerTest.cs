using Application.Contracts.Data;
using Application.Features.Antibiotics.CreateAntibiotic;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.Antibiotics.CreateAntibiotic;

public class CreateAntibioticHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly CreateAntibioticHandler _handler;
    private readonly IDbContext _context;

    public CreateAntibioticHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new CreateAntibioticMapper();
        var logger = new Mock<ILogger<CreateAntibioticHandler>>().Object;

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

    private async Task<AntibioticGroup> SeedGroupAsync(string name, string description, bool softDeleted = false)
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

    # region Happy path

    [Theory]
    [InlineData("Amoxicillin", "Access", "Oral", "500 mg orally every 8 hours")]
    [InlineData("Meropenem", "Watch", "Intravenous", "1 g IV every 8 hours")]
    public async Task CreateAntibiotic_Success(string name, string classification, string route, string standardDose)
    {
        var group = await SeedGroupAsync("Beta-lactams", "Cell wall synthesis inhibitors sharing the beta-lactam ring");

        var result = await _handler.HandleAsync(new CreateAntibioticCommand
        {
            Name = name,
            AntibioticGroupId = group.Id,
            Classification = Enum.Parse<AwareClassification>(classification),
            RouteOfAdministration = Enum.Parse<RouteOfAdministration>(route),
            StandardDose = standardDose,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Equal(Status.Created, result.StatusCode);

        Assert.NotEqual(Guid.Empty, result.Data.Id);

        // Verify through a fresh context so the change tracker cannot mask a failed commit
        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.Antibiotics.SingleAsync(
            x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);
        Assert.Equal(name, saved.Name);
        Assert.Equal(group.Id, saved.AntibioticGroupId);

        var dosage = Assert.Single(await freshContext.Dosages
            .Where(x => x.AntibioticId == result.Data.Id)
            .ToListAsync(TestContext.Current.CancellationToken));
        Assert.Contains(dosage.Id, saved.DosageIds);
        Assert.Equal(standardDose, dosage.Dose);
        Assert.Equal(Enum.Parse<RouteOfAdministration>(route), dosage.RouteOfAdministration);
        Assert.Null(dosage.Crcl);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task CreateAntibiotic_UnknownGroup_Fail()
    {
        var unknownGroupId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new CreateAntibioticCommand
            {
                Name = "Ciprofloxacin",
                AntibioticGroupId = unknownGroupId,
                Classification = AwareClassification.Watch,
                RouteOfAdministration = RouteOfAdministration.Oral,
                StandardDose = "500 mg orally every 12 hours",
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // Nothing must be created when the group does not exist
        Assert.Equal(0, await _context.Antibiotics.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await _context.Dosages.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAntibiotic_SoftDeletedGroup_Fail()
    {
        // A soft-deleted group is hidden by the query filter, so referencing it
        // must be rejected just like an unknown group
        var deletedGroup = await SeedGroupAsync("Polypeptides",
            "Discontinued classification branch", softDeleted: true);

        var result = await _handler.HandleAsync(
            new CreateAntibioticCommand
            {
                Name = "Colistin",
                AntibioticGroupId = deletedGroup.Id,
                Classification = AwareClassification.Reserve,
                RouteOfAdministration = RouteOfAdministration.Intravenous,
                StandardDose = "2.5 mg/kg IV every 12 hours",
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    # endregion
}
