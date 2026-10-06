using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.Pathogens.CreatePathogen;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.Pathogens.CreatePathogen
{
    public class CreatePathogenHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly CreatePathogenHandler _handler;
        private readonly IDbContext _context;

        public CreatePathogenHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
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

        #region Happy path

        [Theory]
        [InlineData("Pseudomonas arguresia", "blablabla", true)]
        [InlineData("H. influzae", "blablabla", false)]
        [InlineData("abc", "not blablabla", true)]
        public async Task CreatePathogen_Success(string name, string description, bool isAtypical)
        {
            var result = await _handler.HandleAsync(new CreatePathogenCommand
            {
                Name = name,
                Description = description,
                IsAtypical = isAtypical
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.True(result.Data.Id != Guid.Empty);

            // Verify through a fresh context so the change tracker of the saving context
            // cannot mask whether the row was truly committed
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.Pathogens.SingleAsync(
                p => p.Name == name && p.Description == description,
                TestContext.Current.CancellationToken);

            Assert.Equal(result.Data.Id, saved.Id);
        }

        #endregion
    }
}
