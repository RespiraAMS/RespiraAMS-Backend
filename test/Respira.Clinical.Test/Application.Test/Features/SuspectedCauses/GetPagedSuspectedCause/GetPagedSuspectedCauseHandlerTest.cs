using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.SuspectedCauses.GetPagedSuspectedCause;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Infrastructure.Data;
using Respira.Clinical.Infrastructure.Mapper;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.SuspectedCauses.GetPagedSuspectedCause
{
    public class GetPagedSuspectedCauseHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly GetPagedSuspectedCauseHandler _handler;
        private readonly IDbContext _context;

        public GetPagedSuspectedCauseHandlerTest(PostgresFixture fixture)
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
            // Causes reference pathogens through FKs, so delete them first.
            // IgnoreQueryFilters is needed because soft-deleted rows are hidden by the
            // query filter but still occupy the table
            await _context.SuspectedCauses.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Pathogens.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        /*
         * Seeds five causes across two pathogens (Klebsiella on odd 1-based positions,
         * Pseudomonas on even ones) whose CreatedAt are spaced 1 minute apart so that
         * the CreatedAt-descending order of the handler is deterministic: the last list
         * element is the most recent cause. Severity and treatment site cycle through
         * Mild/Moderate/Severe and Outpatient/Inpatient/IntensiveCareUnit per 1-based
         * position, so every filter value matches a predictable subset of the rows
         */
        private async Task<List<SuspectedCause>> SeedNumberedCausesAsync(int count)
        {
            var baseTime = DateTimeOffset.UtcNow;

            var klebsiella = new Pathogen
            {
                Name = "Klebsiella pneumoniae",
                Description = "Gram-negative bacillus",
                IsAtypical = true,
            };
            var pseudomonas = new Pathogen
            {
                Name = "Pseudomonas aeruginosa",
                Description = "Gram-negative rod",
                IsAtypical = false,
            };
            await _context.Pathogens.AddRangeAsync([klebsiella, pseudomonas], TestContext.Current.CancellationToken);

            var severities = new[] { Severity.Mild, Severity.Moderate, Severity.Severe };
            var treatmentSites = new[] { TreatmentSite.Outpatient, TreatmentSite.Inpatient, TreatmentSite.IntensiveCareUnit };
            var seeded = Enumerable.Range(1, count)
                .Select(i => new SuspectedCause
                {
                    PathogenId = i % 2 == 0 ? pseudomonas.Id : klebsiella.Id,
                    Severity = severities[(i - 1) % severities.Length],
                    TreatmentSite = treatmentSites[(i - 1) % treatmentSites.Length],
                    CreatedAt = baseTime.AddMinutes(-count + i),
                })
                .ToList();

            await _context.SuspectedCauses.AddRangeAsync(seeded, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return seeded;
        }

        #region Happy path

        [Fact]
        public async Task GetPagedSuspectedCause_NoFilter_FirstPageNewestFirst_Success()
        {
            // 5 items with page size 2 -> pages of [2, 2, 1]
            var seeded = await SeedNumberedCausesAsync(5);

            var result = await _handler.HandleAsync(new GetPagedSuspectedCauseQuery
            {
                Param = new PaginationParam { Page = 1, Size = 2 },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var items = result.Data.Items.ToList();
            Assert.Equal([seeded[4].Id, seeded[3].Id], [.. items.Select(x => x.Id)]);

            // The projection carries the pathogen join and the business data through
            Assert.Equal(seeded[4].PathogenId, items[0].PathogenId);
            Assert.Equal("Klebsiella pneumoniae", items[0].PathogenName);
            Assert.Equal(Severity.Moderate, items[0].Severity);
            Assert.Equal(TreatmentSite.Inpatient, items[0].TreatmentSite);
            Assert.Equal(seeded[3].PathogenId, items[1].PathogenId);
            Assert.Equal("Pseudomonas aeruginosa", items[1].PathogenName);

            Assert.Equal(1, result.Data.Metadata.CurrentPage);
            Assert.Equal(2, result.Data.Metadata.PageSize);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.Equal(3, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasPreviousPage);
            Assert.True(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task GetPagedSuspectedCause_MiddlePage_HasBothNeighbors_Success()
        {
            var seeded = await SeedNumberedCausesAsync(5);

            var result = await _handler.HandleAsync(new GetPagedSuspectedCauseQuery
            {
                Param = new PaginationParam { Page = 2, Size = 2 },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Equal([seeded[2].Id, seeded[1].Id], [.. result.Data.Items.Select(x => x.Id)]);
            Assert.True(result.Data.Metadata.HasPreviousPage);
            Assert.True(result.Data.Metadata.HasNextPage);
            Assert.Equal(2, result.Data.Metadata.CurrentPage);
        }

        [Fact]
        public async Task GetPagedSuspectedCause_LastPartialPage_HasNoNext_Success()
        {
            // Upper boundary page: only 1 leftover item and no next page
            var seeded = await SeedNumberedCausesAsync(5);

            var result = await _handler.HandleAsync(new GetPagedSuspectedCauseQuery
            {
                Param = new PaginationParam { Page = 3, Size = 2 },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal(seeded[0].Id, item.Id);
            Assert.Equal("Klebsiella pneumoniae", item.PathogenName);
            Assert.True(result.Data.Metadata.HasPreviousPage);
            Assert.False(result.Data.Metadata.HasNextPage);
            Assert.Equal(3, result.Data.Metadata.CurrentPage);
        }

        /*=== filter ===*/

        [Fact]
        public async Task GetPagedSuspectedCause_PathogenIdFilter_ReturnsOnlyThatPathogen_Success()
        {
            var seeded = await SeedNumberedCausesAsync(5);
            var klebsiellaId = seeded[0].PathogenId;

            var result = await _handler.HandleAsync(new GetPagedSuspectedCauseQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new SuspectedCauseFilter { PathogenId = klebsiellaId },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            // Odd positions belong to Klebsiella, newest first
            Assert.Equal([seeded[4].Id, seeded[2].Id, seeded[0].Id], [.. result.Data.Items.Select(x => x.Id)]);
            Assert.All(result.Data.Items, x =>
            {
                Assert.Equal(klebsiellaId, x.PathogenId);
                Assert.Equal("Klebsiella pneumoniae", x.PathogenName);
            });
            Assert.Equal(3, result.Data.Metadata.TotalItemCount);
            Assert.False(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task GetPagedSuspectedCause_SeverityFilter_Success()
        {
            var seeded = await SeedNumberedCausesAsync(5);

            var result = await _handler.HandleAsync(new GetPagedSuspectedCauseQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new SuspectedCauseFilter { Severity = Severity.Mild },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            // Mild lands on list indices 0 and 3 of the seeded list
            Assert.Equal([seeded[3].Id, seeded[0].Id], [.. result.Data.Items.Select(x => x.Id)]);
            Assert.All(result.Data.Items, x => Assert.Equal(Severity.Mild, x.Severity));
            Assert.Equal(2, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedSuspectedCause_TreatmentSiteFilter_Success()
        {
            var seeded = await SeedNumberedCausesAsync(5);

            var result = await _handler.HandleAsync(new GetPagedSuspectedCauseQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new SuspectedCauseFilter { TreatmentSite = TreatmentSite.IntensiveCareUnit },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal(seeded[2].Id, item.Id);
            Assert.Equal(Severity.Severe, item.Severity);
            Assert.Equal(TreatmentSite.IntensiveCareUnit, item.TreatmentSite);
            Assert.Equal(1, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedSuspectedCause_CombinedFilters_AppliedTogether_Success()
        {
            var seeded = await SeedNumberedCausesAsync(5);

            var result = await _handler.HandleAsync(new GetPagedSuspectedCauseQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new SuspectedCauseFilter
                {
                    PathogenId = seeded[0].PathogenId,
                    TreatmentSite = TreatmentSite.Inpatient,
                },
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            // Klebsiella covers list indices 0, 2, 4 and Inpatient covers 1, 4 -> only 4 remains
            var item = Assert.Single(result.Data.Items);
            Assert.Equal(seeded[4].Id, item.Id);
            Assert.Equal(Severity.Moderate, item.Severity);
            Assert.Equal(1, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedSuspectedCause_FilterMatchesNothing_ReturnsEmpty()
        {
            await SeedNumberedCausesAsync(5);

            var result = await _handler.HandleAsync(new GetPagedSuspectedCauseQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new SuspectedCauseFilter { PathogenId = Guid.CreateVersion7() },
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
