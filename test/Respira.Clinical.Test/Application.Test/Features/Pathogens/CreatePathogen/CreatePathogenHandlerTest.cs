using Application.Contracts.Data;
using Application.Features.Pathogens.CreatePathogen;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Test.Features.Pathogens.CreatePathogen;

public class CreatePathogenHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly CreatePathogenHandler _handler;
    private readonly IDbContext _context;

    public CreatePathogenHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new CreatePathogenMapper();
        var logger = new Mock<ILogger<CreatePathogenHandler>>().Object;

        // Initialize handler
        _handler = new(_context, mapper, logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        // Clear leftover data so the SingleAsync assertion is deterministic across runs
        await _context.Pathogens.ExecuteDeleteAsync(TestContext.Current.CancellationToken);

    }

    # region Happy path

    [Theory]
    [InlineData("Pseudomonas arguresia", "blablabla")]
    [InlineData("H. influzae", "blablabla")]
    [InlineData("abc", "not blablabla")]
    public async Task CreatePathogen_Success(string name, string description)
    {
        var result = await _handler.HandleAsync(new CreatePathogenCommand
        {
            Name = name,
            Description = description,
        }, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.True(result.Id != Guid.Empty);

        // Verify through a fresh context so the change tracker of the saving context
        // cannot mask whether the row was truly committed
        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.Pathogens.SingleAsync(
            p => p.Name == name && p.Description == description,
            TestContext.Current.CancellationToken);

        Assert.Equal(result.Id, saved.Id);
    }

    # endregion
}
