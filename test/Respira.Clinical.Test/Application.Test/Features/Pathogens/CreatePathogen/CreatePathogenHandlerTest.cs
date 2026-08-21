using Application.Contracts.Data;
using Application.Features.Pathogens.CreatePathogen;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Test.Features.Pathogens.CreatePathogen;

public class CreatePathogenHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly CreatePathogenHandler _handler;
    private readonly IDbContext _context;

    public CreatePathogenHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(options);
        var mapper = new CreatePathogenMapper();
        var logger = new Mock<ILogger<CreatePathogenHandler>>().Object;

        // Initialize handler
        _handler = new(_context, mapper, logger);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask InitializeAsync()
    {
        // Clear leftover data so the Count == 1 assertion is deterministic across runs
        await _context.Pathogens.ExecuteDeleteAsync(TestContext.Current.CancellationToken);

    }

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

        // Check if it added to database success
        Assert.Equal(1, await _context.Pathogens.CountAsync(p => p.Name.Equals(name) && p.Description.Equals(description), TestContext.Current.CancellationToken));
    }
}
