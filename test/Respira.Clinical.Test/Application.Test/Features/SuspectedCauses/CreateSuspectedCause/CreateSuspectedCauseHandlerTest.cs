using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.SuspectedCauses.CreateSuspectedCause;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.SuspectedCauses.CreateSuspectedCause
{
    public class CreateSuspectedCauseHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly CreateSuspectedCauseHandler _handler;
        private readonly IDbContext _context;

        public CreateSuspectedCauseHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var mapper = new CreateSuspectedCauseMapper();
            var logger = new Mock<ILogger<CreateSuspectedCauseHandler>>().Object;

            // Initialize handler
            _handler = new(_context, mapper, logger);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Suspected causes reference pathogens through FKs, so delete them first.
            // IgnoreQueryFilters is needed because soft-deleted rows are hidden by the
            // query filter but still occupy the table
            await _context.SuspectedCauses.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Pathogens.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        private async Task<Guid> SeedAsync(bool softDeletedPathogen = false)
        {
            var pathogen = new Pathogen
            {
                Name = "Klebsiella pneumoniae",
                Description = "Gram-negative bacillus",
                IsAtypical = true,
                IsDeleted = softDeletedPathogen,
                DeletedAt = softDeletedPathogen ? DateTimeOffset.UtcNow : null,
            };

            await _context.Pathogens.AddAsync(pathogen, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            return pathogen.Id;
        }

        #region Happy path

        [Fact]
        public async Task CreateCause_Success()
        {
            var pathogenId = await SeedAsync();

            var result = await _handler.HandleAsync(new CreateSuspectedCauseCommand
            {
                PathogenId = pathogenId,
                Severity = Severity.Moderate,
                TreatmentSite = TreatmentSite.Inpatient,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.NotEqual(Guid.Empty, result.Data.Id);

            // Verify through a fresh context so the change tracker cannot mask a failed commit
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.SuspectedCauses
                .SingleAsync(x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);
            Assert.Equal(pathogenId, saved.PathogenId);
            Assert.Equal(Severity.Moderate, saved.Severity);
            Assert.Equal(TreatmentSite.Inpatient, saved.TreatmentSite);
        }

        [Fact]
        public async Task CreateCause_SamePathogenDifferentAttributes_Success()
        {
            /*
             * The uniqueness business rule covers the whole tuple
             * (pathogen, severity, treatment site): the same pair can carry
             * several cause rows as long as severity or treatment site differs
             */
            var pathogenId = await SeedAsync();
            await _context.SuspectedCauses.AddAsync(new SuspectedCause
            {
                PathogenId = pathogenId,
                Severity = Severity.Mild,
                TreatmentSite = TreatmentSite.Outpatient,
            }, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(new CreateSuspectedCauseCommand
            {
                PathogenId = pathogenId,
                Severity = Severity.Severe,
                TreatmentSite = TreatmentSite.IntensiveCareUnit,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.NotEqual(Guid.Empty, result.Data.Id);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(2, await freshContext.SuspectedCauses
                .CountAsync(x => x.PathogenId == pathogenId, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateCause_SoftDeletedCauseCanBeRecreated_Success()
        {
            /*
             * Uniqueness is enforced at the application level (per the handler docs,
             * soft delete replaces a DB UNIQUE index), so a soft-deleted cause is hidden
             * by the query filter and the same tuple can be created again
             */
            var pathogenId = await SeedAsync();
            var original = new SuspectedCause
            {
                PathogenId = pathogenId,
                Severity = Severity.Mild,
                TreatmentSite = TreatmentSite.Outpatient,
                IsDeleted = true,
                DeletedAt = DateTimeOffset.UtcNow,
            };
            await _context.SuspectedCauses.AddAsync(original, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(new CreateSuspectedCauseCommand
            {
                PathogenId = pathogenId,
                Severity = Severity.Mild,
                TreatmentSite = TreatmentSite.Outpatient,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.NotEqual(Guid.Empty, result.Data.Id);
            Assert.NotEqual(original.Id, result.Data.Id);
            Assert.Equal(2, await _context.SuspectedCauses.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        #endregion

        #region Fail path


        [Fact]
        public async Task CreateCause_PathogenNotFound_Fail()
        {
            await SeedAsync();
            var unknownPathogenId = Guid.CreateVersion7();

            var result = await _handler.HandleAsync(
                new CreateSuspectedCauseCommand
                {
                    PathogenId = unknownPathogenId,
                    Severity = Severity.Moderate,
                    TreatmentSite = TreatmentSite.Inpatient,
                }, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            Assert.Equal(0, await _context.SuspectedCauses.CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateCause_SoftDeletedPathogen_Fail()
        {
            // A soft-deleted pathogen is hidden by the query filter, so referencing it
            // must be rejected just like an unknown pathogen
            var deletedPathogenId = await SeedAsync(softDeletedPathogen: true);

            var result = await _handler.HandleAsync(
                new CreateSuspectedCauseCommand
                {
                    PathogenId = deletedPathogenId,
                    Severity = Severity.Moderate,
                    TreatmentSite = TreatmentSite.Inpatient,
                }, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task CreateCause_DuplicateTuple_Fail()
        {
            /*
             * Business rule: the exact tuple (pathogen, severity, treatment
             * site) can only exist once among the active causes
             */
            var pathogenId = await SeedAsync();
            await _context.SuspectedCauses.AddAsync(new SuspectedCause
            {
                PathogenId = pathogenId,
                Severity = Severity.Mild,
                TreatmentSite = TreatmentSite.Outpatient,
            }, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(
                new CreateSuspectedCauseCommand
                {
                    PathogenId = pathogenId,
                    Severity = Severity.Mild,
                    TreatmentSite = TreatmentSite.Outpatient,
                }, TestContext.Current.CancellationToken);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The duplicate must not be persisted
            Assert.Equal(1, await _context.SuspectedCauses.CountAsync(TestContext.Current.CancellationToken));
        }

        #endregion
    }
}
