using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Pathogens.GetPagedPathogen;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Infrastructure.Data;
using Respira.Clinical.Infrastructure.Mapper;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.Pathogens.GetPagedPathogen
{
    public class GetPagedPathogensHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly GetPagedPathogensHandler _handler;
        private readonly IDbContext _context;

        public GetPagedPathogensHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
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
            await _context.Pathogens.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        /*
         * Seeds pathogens whose CreatedAt are spaced 1 minute apart so that the
         * CreatedAt-descending order of the handler is deterministic. Higher suffix
         * means newer: Pathogen-5 is the most recent one
         */
        private async Task<List<Pathogen>> SeedNumberedPathogensAsync(int count, bool isAtypical = true)
        {
            var baseTime = DateTimeOffset.UtcNow;
            var seeded = Enumerable.Range(1, count)
                .Select(i => new Pathogen
                {
                    Name = $"Pathogen-{i}",
                    Description = $"Description of pathogen {i}",
                    IsAtypical = isAtypical,
                    CreatedAt = baseTime.AddMinutes(-count + i),
                })
                .ToList();

            await _context.Pathogens.AddRangeAsync(seeded, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return seeded;
        }

        #region Happy path

        [Fact]
        public async Task GetPagedPathogens_NoFilter_FirstPageNewestFirst_Success()
        {
            // 5 items with page size 2 -> pages of [2, 2, 1]
            await SeedNumberedPathogensAsync(5);

            var result = await _handler.HandleAsync(new GetPagedPathogenQuery
            {
                Param = new PaginationParam { Page = 1, Size = 2 },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Equal(["Pathogen-5", "Pathogen-4"], [.. result.Data.Items.Select(x => x.Name)]);

            Assert.Equal(1, result.Data.Metadata.CurrentPage);
            Assert.Equal(2, result.Data.Metadata.PageSize);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.Equal(3, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasPreviousPage);
            Assert.True(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task GetPagedPathogens_MiddlePage_HasBothNeighbors_Success()
        {
            await SeedNumberedPathogensAsync(5);

            var result = await _handler.HandleAsync(new GetPagedPathogenQuery
            {
                Param = new PaginationParam { Page = 2, Size = 2 },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Equal(["Pathogen-3", "Pathogen-2"], [.. result.Data.Items.Select(x => x.Name)]);
            Assert.True(result.Data.Metadata.HasPreviousPage);
            Assert.True(result.Data.Metadata.HasNextPage);
            Assert.Equal(2, result.Data.Metadata.CurrentPage);
        }

        [Fact]
        public async Task GetPagedPathogens_LastPartialPage_HasNoNext_Success()
        {
            // Upper boundary page: only 1 leftover item and no next page
            await SeedNumberedPathogensAsync(5);

            var result = await _handler.HandleAsync(new GetPagedPathogenQuery
            {
                Param = new PaginationParam { Page = 3, Size = 2 },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Pathogen-1", item.Name);
            Assert.True(result.Data.Metadata.HasPreviousPage);
            Assert.False(result.Data.Metadata.HasNextPage);
            Assert.Equal(3, result.Data.Metadata.CurrentPage);
        }

        /*=== filter ===*/

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetPagedPathogens_FilterIsAtypical_ReturnOnlyThatPathogen_Success(bool isAtypical)
        {
            await SeedNumberedPathogensAsync(5, true);
            await SeedNumberedPathogensAsync(3, false);

            var result = await _handler.HandleAsync(new GetPagedPathogenQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new PathogenFilter { IsAtypical = isAtypical },
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.All(result.Data.Items, x => Assert.Equal(isAtypical, x.IsAtypical));
            Assert.Equal(isAtypical switch
            {
                true => 5,
                false => 3,
            }, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedPathogens_FilterMatchesNothing_ReturnsEmpty()
        {
            // The seed create 
            await SeedNumberedPathogensAsync(3);

            var result = await _handler.HandleAsync(new GetPagedPathogenQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new PathogenFilter { IsAtypical = false },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        #endregion
    }
}
