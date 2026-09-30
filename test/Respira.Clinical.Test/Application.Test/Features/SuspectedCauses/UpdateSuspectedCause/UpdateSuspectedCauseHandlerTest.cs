using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.SuspectedCauses.UpdateSuspectedCause;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.SuspectedCauses.UpdateSuspectedCause
{
    public class UpdateSuspectedCauseHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly UpdateSuspectedCauseHandler _handler;
        private readonly IDbContext _context;

        public UpdateSuspectedCauseHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var mapper = new UpdateSuspectedCauseMapper();
            var logger = new Mock<ILogger<UpdateSuspectedCauseHandler>>().Object;

            // Initialize handler
            _handler = new(_context, mapper, logger);
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
         * Seeds the first disease+pathogen pair (community-acquired pneumonia with a
         * realistic CURB-65 based ICU threshold, Klebsiella) plus the given causes for
         * that pair. When secondPairCauses is provided, a second disease+pathogen pair
         * (hospital-acquired pneumonia, Pseudomonas) is seeded with those causes.
         * Flags allow seeding a soft-deleted first cause to exercise the query filter
         */
        private async Task<List<SuspectedCause>> SeedAsync(
            List<(Severity Severity, TreatmentSite TreatmentSite)> causes,
            bool softDeletedFirst = false,
            List<(Severity Severity, TreatmentSite TreatmentSite)>? secondPairCauses = null)
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

            if (secondPairCauses is not null)
            {
                var secondPathogen = new Pathogen
                {
                    Name = "Pseudomonas aeruginosa",
                    Description = "Gram-negative rod",
                    IsAtypical = false
                };
                await _context.Pathogens.AddAsync(secondPathogen, TestContext.Current.CancellationToken);

                foreach (var (severity, treatmentSite) in secondPairCauses)
                {
                    var cause = new SuspectedCause
                    {
                        PathogenId = secondPathogen.Id,
                        Severity = severity,
                        TreatmentSite = treatmentSite,
                    };
                    seeded.Add(cause);
                    await _context.SuspectedCauses.AddAsync(cause, TestContext.Current.CancellationToken);
                }
            }

            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return seeded;
        }

        #region Happy path

        [Fact]
        public async Task UpdateCause_Success()
        {
            var causes = await SeedAsync(
            [
                (Severity.Mild, TreatmentSite.Outpatient),
            ]);
            var target = causes[0];
            var updatedBefore = DateTimeOffset.UtcNow;

            var result = await _handler.HandleAsync(new UpdateSuspectedCauseCommand
            {
                Id = target.Id,
                PathogenId = target.PathogenId,
                Severity = Severity.Severe,
                TreatmentSite = TreatmentSite.IntensiveCareUnit,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            // Verify through a fresh context so the change tracker cannot mask a failed commit
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.SuspectedCauses
                .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken);
            Assert.Equal(target.PathogenId, saved.PathogenId);
            Assert.Equal(Severity.Severe, saved.Severity);
            Assert.Equal(TreatmentSite.IntensiveCareUnit, saved.TreatmentSite);
            Assert.InRange(
                saved.UpdatedAt.ToUnixTimeMilliseconds(),
                updatedBefore.AddSeconds(-5).ToUnixTimeMilliseconds(),
                DateTimeOffset.UtcNow.AddSeconds(5).ToUnixTimeMilliseconds());

            // No other cause may appear or disappear
            Assert.Equal(1, await freshContext.SuspectedCauses.CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task UpdateCause_MoveToOtherPathogen_Success()
        {
            /*
             * The cause may be re-assigned to another pathogen as long as the target
             * pathogen exists and the new tuple does not collide within the old pair
             */
            var causes = await SeedAsync(
            [
                (Severity.Mild, TreatmentSite.Outpatient),
            ], secondPairCauses:
            [
                (Severity.Severe, TreatmentSite.IntensiveCareUnit),
            ]);
            var target = causes[0];
            var newPathogenId = causes[1].PathogenId;
            Assert.NotEqual(target.PathogenId, newPathogenId);

            var result = await _handler.HandleAsync(new UpdateSuspectedCauseCommand
            {
                Id = target.Id,
                PathogenId = newPathogenId,
                Severity = Severity.Moderate,
                TreatmentSite = TreatmentSite.Inpatient,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.SuspectedCauses
                .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken);
            Assert.Equal(newPathogenId, saved.PathogenId);
            Assert.Equal(Severity.Moderate, saved.Severity);
            Assert.Equal(TreatmentSite.Inpatient, saved.TreatmentSite);

            // The sibling of the second pair must stay untouched
            var sibling = await freshContext.SuspectedCauses
                .SingleAsync(x => x.Id == causes[1].Id, TestContext.Current.CancellationToken);
            Assert.Equal(Severity.Severe, sibling.Severity);
            Assert.Equal(TreatmentSite.IntensiveCareUnit, sibling.TreatmentSite);
        }

        [Fact]
        public async Task UpdateCause_SameTupleOnOtherPathogen_Success()
        {
            /*
             * The uniqueness business rule is scoped per pathogen: taking over a
             * (severity, treatment site) tuple that already exists under a different
             * pathogen is allowed
             */
            var causes = await SeedAsync(
            [
                (Severity.Mild, TreatmentSite.Outpatient),
            ], secondPairCauses:
            [
                (Severity.Severe, TreatmentSite.IntensiveCareUnit),
            ]);
            var target = causes[1];

            var result = await _handler.HandleAsync(new UpdateSuspectedCauseCommand
            {
                Id = target.Id,
                PathogenId = target.PathogenId,
                Severity = Severity.Mild,
                TreatmentSite = TreatmentSite.Outpatient,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.SuspectedCauses
                .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken);
            Assert.Equal(Severity.Mild, saved.Severity);
            Assert.Equal(TreatmentSite.Outpatient, saved.TreatmentSite);
            Assert.Equal(2, await freshContext.SuspectedCauses.CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task UpdateCause_SoftDeletedDuplicate_Success()
        {
            /*
             * Uniqueness is enforced at the application level (soft delete replaces a
             * DB UNIQUE index), so a soft-deleted twin is hidden by the query filter
             * and must not block the update
             */
            var causes = await SeedAsync(
            [
                (Severity.Mild, TreatmentSite.Outpatient),
                (Severity.Severe, TreatmentSite.IntensiveCareUnit),
            ], softDeletedFirst: true);
            var target = causes[1];

            var result = await _handler.HandleAsync(new UpdateSuspectedCauseCommand
            {
                Id = target.Id,
                PathogenId = target.PathogenId,
                Severity = Severity.Mild,
                TreatmentSite = TreatmentSite.Outpatient,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.SuspectedCauses
                .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken);
            Assert.Equal(Severity.Mild, saved.Severity);
            Assert.Equal(TreatmentSite.Outpatient, saved.TreatmentSite);

            // The soft-deleted twin must remain soft-deleted (hidden by the query filter)
            Assert.Equal(1, await freshContext.SuspectedCauses.CountAsync(TestContext.Current.CancellationToken));
            Assert.Equal(2, await freshContext.SuspectedCauses.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task UpdateCause_CauseNotFound_Fail()
        {
            var causes = await SeedAsync(
            [
                (Severity.Mild, TreatmentSite.Outpatient),
            ]);
            var unknownId = Guid.CreateVersion7();

            var result = await _handler.HandleAsync(
                new UpdateSuspectedCauseCommand
                {
                    Id = unknownId,
                    PathogenId = causes[0].PathogenId,
                    Severity = Severity.Severe,
                    TreatmentSite = TreatmentSite.IntensiveCareUnit,
                }, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The seeded cause must stay untouched and no row may be created
            await using var freshContext = new ClinicalDbContext(_options);
            var untouched = await freshContext.SuspectedCauses
                .SingleAsync(x => x.Id == causes[0].Id, TestContext.Current.CancellationToken);
            Assert.Equal(Severity.Mild, untouched.Severity);
            Assert.Equal(TreatmentSite.Outpatient, untouched.TreatmentSite);
            Assert.Equal(1, await freshContext.SuspectedCauses.CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task UpdateCause_SoftDeletedCause_Fail()
        {
            // A soft-deleted cause is already hidden by the query filter, so updating it
            // again must be rejected just like an unknown cause
            var causes = await SeedAsync(
            [
                (Severity.Mild, TreatmentSite.Outpatient),
            ], softDeletedFirst: true);

            var result = await _handler.HandleAsync(
                new UpdateSuspectedCauseCommand
                {
                    Id = causes[0].Id,
                    PathogenId = causes[0].PathogenId,
                    Severity = Severity.Severe,
                    TreatmentSite = TreatmentSite.IntensiveCareUnit,
                }, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The already-deleted row must keep its original business data
            await using var freshContext = new ClinicalDbContext(_options);
            var stillDeleted = await freshContext.SuspectedCauses.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == causes[0].Id, TestContext.Current.CancellationToken);
            Assert.True(stillDeleted.IsDeleted);
            Assert.NotNull(stillDeleted.DeletedAt);
            Assert.Equal(Severity.Mild, stillDeleted.Severity);
            Assert.Equal(TreatmentSite.Outpatient, stillDeleted.TreatmentSite);
        }

        [Fact]
        public async Task UpdateCause_PathogenNotFound_Fail()
        {
            var causes = await SeedAsync(
            [
                (Severity.Mild, TreatmentSite.Outpatient),
            ]);
            var unknownPathogenId = Guid.CreateVersion7();

            var result = await _handler.HandleAsync(
                new UpdateSuspectedCauseCommand
                {
                    Id = causes[0].Id,
                    PathogenId = unknownPathogenId,
                    Severity = Severity.Severe,
                    TreatmentSite = TreatmentSite.IntensiveCareUnit,
                }, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The target must stay untouched when the new pathogen does not exist
            await using var freshContext = new ClinicalDbContext(_options);
            var untouched = await freshContext.SuspectedCauses
                .SingleAsync(x => x.Id == causes[0].Id, TestContext.Current.CancellationToken);
            Assert.Equal(causes[0].PathogenId, untouched.PathogenId);
            Assert.Equal(Severity.Mild, untouched.Severity);
            Assert.Equal(TreatmentSite.Outpatient, untouched.TreatmentSite);
        }

        [Fact]
        public async Task UpdateCause_SoftDeletedPathogen_Fail()
        {
            // A soft-deleted pathogen is hidden by the query filter, so referencing it
            // must be rejected just like an unknown pathogen
            var causes = await SeedAsync(
            [
                (Severity.Mild, TreatmentSite.Outpatient),
            ]);
            var pathogen = await _context.Pathogens
                .SingleAsync(x => x.Id == causes[0].PathogenId, TestContext.Current.CancellationToken);
            pathogen.IsDeleted = true;
            pathogen.DeletedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(
                new UpdateSuspectedCauseCommand
                {
                    Id = causes[0].Id,
                    PathogenId = pathogen.Id,
                    Severity = Severity.Severe,
                    TreatmentSite = TreatmentSite.IntensiveCareUnit,
                }, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The target must stay untouched when the new pathogen is soft-deleted
            await using var freshContext = new ClinicalDbContext(_options);
            var untouched = await freshContext.SuspectedCauses
                .SingleAsync(x => x.Id == causes[0].Id, TestContext.Current.CancellationToken);
            Assert.Equal(Severity.Mild, untouched.Severity);
            Assert.Equal(TreatmentSite.Outpatient, untouched.TreatmentSite);
        }

        [Fact]
        public async Task UpdateCause_DuplicateSeveritySite_Fail()
        {
            /*
             * Business rule: within one pathogen, the exact tuple (severity, treatment
             * site) can only exist once among the active causes
             */
            var causes = await SeedAsync(
            [
                (Severity.Mild, TreatmentSite.Outpatient),
                (Severity.Severe, TreatmentSite.IntensiveCareUnit),
            ]);
            var target = causes[1];

            var result = await _handler.HandleAsync(
                new UpdateSuspectedCauseCommand
                {
                    Id = target.Id,
                    PathogenId = target.PathogenId,
                    Severity = Severity.Mild,
                    TreatmentSite = TreatmentSite.Outpatient,
                }, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Neither the target nor the existing tuple may change
            await using var freshContext = new ClinicalDbContext(_options);
            var untouched = await freshContext.SuspectedCauses
                .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken);
            Assert.Equal(Severity.Severe, untouched.Severity);
            Assert.Equal(TreatmentSite.IntensiveCareUnit, untouched.TreatmentSite);
            Assert.Equal(2, await freshContext.SuspectedCauses.CountAsync(TestContext.Current.CancellationToken));
        }

        #endregion
    }
}
