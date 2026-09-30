using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.SuspectedCauses.DeleteSuspectedCause;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.SuspectedCauses.DeleteSuspectedCause
{
    public class DeleteSuspectedCauseHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly DeleteSuspectedCauseHandler _handler;
        private readonly IDbContext _context;

        public DeleteSuspectedCauseHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var logger = new Mock<ILogger<DeleteSuspectedCauseHandler>>().Object;

            // Initialize handler
            _handler = new(_context, logger);
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
         * Seeds one disease (community-acquired pneumonia with a realistic CURB-65 based
         * ICU threshold), one pathogen and the given causes. The flag allows seeding a
         * soft-deleted first cause to exercise the query filter
         */
        private async Task<List<SuspectedCause>> SeedAsync(
            List<(Severity Severity, TreatmentSite TreatmentSite)> causes,
            bool softDeletedFirst = false)
        {
            var pathogen = new Pathogen
            {
                Name = "Klebsiella pneumoniae",
                Description = "Gram-negative bacillus",
                IsAtypical = true,
            };
            await _context.Pathogens.AddAsync(pathogen, TestContext.Current.CancellationToken);

            var seeded = new List<SuspectedCause>();
            for (var i = 0; i < causes.Count; i++)
            {
                var cause = new SuspectedCause
                {
                    PathogenId = pathogen.Id,
                    Severity = causes[i].Severity,
                    TreatmentSite = causes[i].TreatmentSite,
                    IsDeleted = i == 0 && softDeletedFirst,
                    DeletedAt = i == 0 && softDeletedFirst ? DateTimeOffset.UtcNow : null,
                };
                seeded.Add(cause);
                await _context.SuspectedCauses.AddAsync(cause, TestContext.Current.CancellationToken);
            }

            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return seeded;
        }

        #region Happy path

        [Fact]
        public async Task DeleteCause_WithSiblingCause_Success()
        {
            var causes = await SeedAsync(
            [
                (Severity.Mild, TreatmentSite.Outpatient),
                (Severity.Severe, TreatmentSite.IntensiveCareUnit),
            ]);
            var target = causes[0];

            var result = await _handler.HandleAsync(new DeleteSuspectedCauseCommand { Id = target.Id }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            // Deletion is a soft delete: the row must remain with the flags set.
            // Causes carry a !IsDeleted query filter, so IgnoreQueryFilters is required
            // to observe the soft-deleted row
            await using var freshContext = new ClinicalDbContext(_options);
            var deleted = await freshContext.SuspectedCauses.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken);
            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);

            // The stored business data must survive the deletion untouched
            Assert.Equal(Severity.Mild, deleted.Severity);
            Assert.Equal(TreatmentSite.Outpatient, deleted.TreatmentSite);
            Assert.Equal(target.PathogenId, deleted.PathogenId);

            // The default query filter must now hide the deleted cause
            Assert.False(await freshContext.SuspectedCauses
                .AnyAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken));

            // The sibling cause must stay active
            var sibling = await freshContext.SuspectedCauses
                .SingleAsync(x => x.Id == causes[1].Id, TestContext.Current.CancellationToken);
            Assert.False(sibling.IsDeleted);
        }

        [Fact]
        public async Task DeleteCause_OnlyRemainingCause_Success()
        {
            // Lower boundary: deleting the only cause of the disease+pathogen pair is a
            // plain soft delete with no business rule forbidding it
            var causes = await SeedAsync(
            [
                (Severity.Moderate, TreatmentSite.Inpatient),
            ]);

            var result = await _handler.HandleAsync(new DeleteSuspectedCauseCommand { Id = causes[0].Id },
                TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            Assert.Equal(0, await _context.SuspectedCauses.CountAsync(TestContext.Current.CancellationToken));
            Assert.Equal(1, await _context.SuspectedCauses.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task DeleteCause_NotFound_Fail()
        {
            var unknownId = Guid.CreateVersion7();

            var result = await _handler.HandleAsync(
                new DeleteSuspectedCauseCommand { Id = unknownId },
                TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Nothing must exist when the target was never there
            Assert.Equal(0, await _context.SuspectedCauses.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteCause_SoftDeletedCause_Fail()
        {
            // A soft-deleted cause is already hidden by the query filter, so deleting it
            // again must be rejected just like an unknown cause
            var causes = await SeedAsync(
            [
                (Severity.Mild, TreatmentSite.Outpatient),
        ], softDeletedFirst: true);

            var result = await _handler.HandleAsync(
                new DeleteSuspectedCauseCommand { Id = causes[0].Id },
                TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The already-deleted row must keep its original delete timestamp
            // (Postgres timestamp precision is lower than DateTimeOffset ticks)
            await using var freshContext = new ClinicalDbContext(_options);
            var stillDeleted = await freshContext.SuspectedCauses.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == causes[0].Id, TestContext.Current.CancellationToken);
            Assert.True(stillDeleted.IsDeleted);
            Assert.NotNull(stillDeleted.DeletedAt);
            Assert.True((stillDeleted.DeletedAt - causes[0].DeletedAt).GetValueOrDefault().Duration() < TimeSpan.FromSeconds(1));
        }

        #endregion
    }
}
