using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.Antibiotics.GetAntibiotics;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.Antibiotics.GetAntibiotics
{
    public class GetAntibioticsHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly GetAntibioticsHandler _handler;
        private readonly IDbContext _context;

        public GetAntibioticsHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>()
                .UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);

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

        #region Happy path

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
                new() { Name = "Meropenem", AntibioticGroupId = group.Id, Classification = AwareClassification.Watch },
                new() { Name = "Amoxicillin", AntibioticGroupId = group.Id, Classification = AwareClassification.Access },
                new() { Name = "Co-amoxiclav", AntibioticGroupId = group.Id, Classification = AwareClassification.AccessWatch },
            };
            await _context.AntibioticGroups.AddAsync(group, TestContext.Current.CancellationToken);
            await _context.Antibiotics.AddRangeAsync(seeded, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var idByName = seeded.ToDictionary(x => x.Name, x => x.Id);

            var result = await _handler.HandleAsync(new GetAntibioticsQuery(),
                TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(
            [
                "Amoxicillin",
                "Co-amoxiclav",
                "Meropenem",
            ], [.. result.Data.Antibiotics.Select(x => x.Name)]);

            // The projection must keep the ID together with the name
            var amoxicillin = Assert.Single(result.Data.Antibiotics, x => x.Name == "Amoxicillin");
            Assert.Equal(idByName["Amoxicillin"], amoxicillin.Id);
            Assert.All(result.Data.Antibiotics, x => Assert.NotEqual(Guid.Empty, x.Id));
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
                Classification = AwareClassification.Watch,
            };
            var softDeleted = new Antibiotic
            {
                Name = "Telithromycin",
                AntibioticGroupId = group.Id,
                Classification = AwareClassification.Others,
                IsDeleted = true,
                DeletedAt = DateTimeOffset.UtcNow,
            };
            await _context.AntibioticGroups.AddAsync(group, TestContext.Current.CancellationToken);
            await _context.Antibiotics.AddRangeAsync([alive, softDeleted], TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(new GetAntibioticsQuery(),
                TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var item = Assert.Single(result.Data.Antibiotics);
            Assert.Equal("Azithromycin", item.Name);
        }

        /*=== boundary: no data at all ===*/

        [Fact]
        public async Task GetAntibiotics_EmptyDatabase_ReturnsEmpty()
        {
            var result = await _handler.HandleAsync(new GetAntibioticsQuery(),
                TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Empty(result.Data.Antibiotics);
        }

        #endregion
    }
}
