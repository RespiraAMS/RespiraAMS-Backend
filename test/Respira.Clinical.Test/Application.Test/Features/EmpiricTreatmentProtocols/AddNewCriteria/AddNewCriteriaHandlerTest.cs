using Application.Features.EmpiricTreatmentProtocols.AddNewCriteria;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Contracts.Results;
using Range = Domain.Models.Range;

namespace Application.Test.Features.EmpiricTreatmentProtocols.AddNewCriteria;

public class AddNewCriteriaHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly AddNewCriteriaHandler _handler;
    private readonly AppDbContext _context;

    // Reused supporting entity
    private Guid _diseaseId;

    public AddNewCriteriaHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new CreateCriterionMapper();
        var logger = new Mock<ILogger<AddNewCriteriaHandler>>().Object;

        _handler = new(_context, mapper, logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await CleanupAsync();

        var disease = new Disease
        {
            Name = "Community-Acquired Pneumonia",
            Description = "Infection of the lung parenchyma acquired outside of a healthcare setting",
            IcuScoreThreshold = 3,
        };
        _context.Diseases.Add(disease);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _diseaseId = disease.Id;
    }

    private async Task CleanupAsync()
    {
        var protocols = await _context.EmpiricTreatmentProtocols
            .IgnoreQueryFilters()
            .ToListAsync(TestContext.Current.CancellationToken);
        _context.EmpiricTreatmentProtocols.RemoveRange(protocols);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Criteria are standalone rows; remove orphans left by the protocol deletion
        await _context.Criteria.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    private async Task<EmpiricTreatmentProtocol> SeedProtocolAsync(List<Criterion>? preExistingCriteria = null)
    {
        var protocol = new EmpiricTreatmentProtocol
        {
            Name = "IDSA/ATS 2024 CAP Empiric Guidance",
            Issuer = "Infectious Diseases Society of America",
            IssueDate = new DateOnly(2024, 8, 1),
            Version = 3,
            DiseaseId = _diseaseId,
            Severity = Severity.Severe,
            TreatmentSite = TreatmentSite.IntensiveCareUnit,
            SpecialInfectionId = null,
        };
        await _context.EmpiricTreatmentProtocols.AddAsync(protocol, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        if (preExistingCriteria is not null)
        {
            // Persist the criteria rows first, then link them to the protocol so the
            // join table always references an existing criterion row
            _context.Criteria.AddRange(preExistingCriteria);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            protocol.OtherCriteria.AddRange(preExistingCriteria);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        return protocol;
    }

    # region Happy path

    [Fact]
    public async Task AddNewCriteria_BooleanAndNumeric_Success()
    {
        await CleanupAsync();
        var seeded = await SeedProtocolAsync();

        var command = new AddNewCriteriaCommand
        {
            Id = seeded.Id,
            Criteria =
            [
                new() { Name = "Prior history of COPD", Type = CriterionType.Boolean },
                new()
                {
                    Name = "Respiratory rate",
                    Type = CriterionType.Numeric,
                    Value = new Range
                    {
                        Min = 30m,
                        Max = 120m,
                        IsMinExclusive = false,
                        IsMaxExclusive = false,
                        Unit = "breaths/min",
                    },
                },
            ],
        };

        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Created, result.StatusCode);

        // Verify through a fresh context so the change tracker cannot mask a failed commit
        await using var freshContext = new AppDbContext(_options);
        var savedProtocol = await freshContext.EmpiricTreatmentProtocols
            .Include(p => p.OtherCriteria)
            .SingleAsync(p => p.Id == seeded.Id, TestContext.Current.CancellationToken);

        // Business rule: both criteria are linked to the protocol
        Assert.Equal(2, savedProtocol.OtherCriteria.Count);
        Assert.Contains(savedProtocol.OtherCriteria,
            c => c.Name == "Prior history of COPD" && c is BooleanCriterion);
        Assert.Contains(savedProtocol.OtherCriteria,
            c => c.Name == "Respiratory rate" && c is NumericCriterion);

        // Business rule: the criteria rows are persisted with their type-specific data
        var numeric = savedProtocol.OtherCriteria.OfType<NumericCriterion>().Single();
        Assert.Equal(30m, numeric.Value.Min);
        Assert.Equal(120m, numeric.Value.Max);
        Assert.Equal("breaths/min", numeric.Value.Unit);
    }

    [Fact]
    public async Task AddNewCriteria_SingleBoolean_Success()
    {
        await CleanupAsync();
        var seeded = await SeedProtocolAsync();

        var command = new AddNewCriteriaCommand
        {
            Id = seeded.Id,
            Criteria =
            [
                new() { Name = "Fever > 38C", Type = CriterionType.Boolean },
            ],
        };

        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Created, result.StatusCode);

        await using var freshContext = new AppDbContext(_options);
        var savedProtocol = await freshContext.EmpiricTreatmentProtocols
            .Include(p => p.OtherCriteria)
            .SingleAsync(p => p.Id == seeded.Id, TestContext.Current.CancellationToken);

        _ = Assert.Single(savedProtocol.OtherCriteria);
        Assert.Equal("Fever > 38C", savedProtocol.OtherCriteria[0].Name);
    }

    [Fact]
    public async Task AddNewCriteria_AppendsToExistingCriteria_Success()
    {
        await CleanupAsync();
        var existing = new BooleanCriterion { Name = "Pre-existing comorbidity" };
        var seeded = await SeedProtocolAsync(preExistingCriteria: [existing]);

        var command = new AddNewCriteriaCommand
        {
            Id = seeded.Id,
            Criteria =
            [
                new() { Name = "Prior history of COPD", Type = CriterionType.Boolean },
                new()
                {
                    Name = "Oxygen saturation",
                    Type = CriterionType.Numeric,
                    Value = new Range { Min = 90m, Max = 100m, IsMinExclusive = false, IsMaxExclusive = false, Unit = "%" },
                },
            ],
        };

        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Created, result.StatusCode);

        await using var freshContext = new AppDbContext(_options);
        var savedProtocol = await freshContext.EmpiricTreatmentProtocols
            .Include(p => p.OtherCriteria)
            .SingleAsync(p => p.Id == seeded.Id, TestContext.Current.CancellationToken);

        // Business rule: new criteria are appended, not replacing the existing ones
        Assert.Equal(3, savedProtocol.OtherCriteria.Count);
        Assert.Contains(savedProtocol.OtherCriteria, c => c.Name == "Pre-existing comorbidity");
        Assert.Contains(savedProtocol.OtherCriteria, c => c.Name == "Prior history of COPD");
        Assert.Contains(savedProtocol.OtherCriteria, c => c.Name == "Oxygen saturation");
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task AddNewCriteria_ProtocolNotFound_Fail()
    {
        await CleanupAsync();
        var unknownId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new AddNewCriteriaCommand
            {
                Id = unknownId,
                Criteria =
                [
                    new() { Name = "Prior history of COPD", Type = CriterionType.Boolean },
                ],
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // Nothing must be created when the protocol does not exist
        Assert.Equal(0, await _context.Criteria.IgnoreQueryFilters()
            .CountAsync(TestContext.Current.CancellationToken));
    }

    # endregion
}
