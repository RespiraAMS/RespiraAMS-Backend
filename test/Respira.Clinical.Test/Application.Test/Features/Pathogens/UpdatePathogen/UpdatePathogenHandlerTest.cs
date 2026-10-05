using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.Pathogens.UpdatePathogen;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.Pathogens.UpdatePathogen
{
    public class UpdatePathogenHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly UpdatePathogenHandler _handler;
        private readonly IDbContext _context;

        public UpdatePathogenHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var mapper = new UpdatePathogenMapper();
            var logger = new Mock<ILogger<UpdatePathogenHandler>>().Object;

            // Initialize handler
            _handler = new(_context, mapper, logger);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Clear leftover data so lookups stay deterministic across runs
            await _context.Pathogens.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        private async Task<Pathogen> SeedPathogenAsync(string name, string description, bool isAtypical = true)
        {
            var pathogen = new Pathogen { Name = name, Description = description, IsAtypical = isAtypical };
            await _context.Pathogens.AddAsync(pathogen, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return pathogen;
        }

        #region Happy path

        [Theory]
        [InlineData("Streptococcus pneumoniae", "Gram-positive diplococcus causing pneumonia and meningitis", true)]
        [InlineData("Haemophilus influenzae", "Gram-negative coccobacillus causing respiratory tract infections", false)]
        public async Task UpdatePathogen_Success(string newName, string newDescription, bool newIsAtypical)
        {
            var seeded = await SeedPathogenAsync("Staphylococcus epidermidis", "Coagulase-negative staphylococcus of skin flora");
            var updatedBefore = DateTimeOffset.UtcNow;

            var result = await _handler.HandleAsync(new UpdatePathogenCommand
            {
                Id = seeded.Id,
                Name = newName,
                Description = newDescription,
                IsAtypical = newIsAtypical,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            // Verify through a fresh context so the change tracker of the saving context
            // cannot mask whether the row was truly committed
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.Pathogens.SingleAsync(
                p => p.Id == seeded.Id, TestContext.Current.CancellationToken);

            Assert.Equal(newName, saved.Name);
            Assert.Equal(newDescription, saved.Description);
            Assert.InRange(
                saved.UpdatedAt.ToUnixTimeMilliseconds(),
                updatedBefore.AddSeconds(-5).ToUnixTimeMilliseconds(),
                DateTimeOffset.UtcNow.AddSeconds(5).ToUnixTimeMilliseconds());
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task UpdatePathogen_PathogenNotFound_Fail()
        {
            var unknownId = Guid.CreateVersion7();

            var result = await _handler.HandleAsync(new UpdatePathogenCommand
            {
                Id = unknownId,
                Name = "Bacillus anthracis",
                Description = "Should never be written",
                IsAtypical = true,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Nothing must be created when the target does not exist
            Assert.Equal(0, await _context.Pathogens.CountAsync(TestContext.Current.CancellationToken));
        }

        #endregion
    }
}
