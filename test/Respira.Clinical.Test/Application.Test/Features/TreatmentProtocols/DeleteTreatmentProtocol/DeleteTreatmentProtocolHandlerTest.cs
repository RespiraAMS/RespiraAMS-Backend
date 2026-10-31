using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.TreatmentProtocols.DeleteTreatmentProtocol;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.TreatmentProtocols.DeleteTreatmentProtocol
{
    public class DeleteTreatmentProtocolHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly DeleteTreatmentProtocolHandler _handler;
        private readonly IDbContext _context;

        public DeleteTreatmentProtocolHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var logger = new Mock<ILogger<DeleteTreatmentProtocolHandler>>().Object;

            // Initialize handler
            _handler = new(_context, logger);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Treatments reference protocols through an FK, so delete them first.
            // IgnoreQueryFilters is needed because soft-deleted rows are hidden by
            // the query filter but still occupy the table
            await _context.Treatments.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.TreatmentProtocols.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        #region Seed helpers

        /*
         * Seeds a protocol with the requested number of medicine compositions. Reference
         * data is written through its own context and saved before the handler runs, so
         * the handler context starts clean just like a real HTTP request.
         */
        private async Task<TreatmentProtocol> SeedProtocolAsync(
            int treatmentCount,
            bool softDeleted = false,
            Severity severity = Severity.Moderate,
            TreatmentSite treatmentSite = TreatmentSite.Inpatient)
        {
            var protocol = new TreatmentProtocol
            {
                Severity = severity,
                TreatmentSite = treatmentSite,
                Version = "2026.1",
                Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                IsApplied = true,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
            };

            var treatments = new List<Treatment>();
            for (var i = 0; i < treatmentCount; i++)
            {
                treatments.Add(new Treatment
                {
                    TreatmentProtocolId = protocol.Id,
                    IsDeleted = softDeleted,
                    DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
                });
            }

            await using var seedContext = new ClinicalDbContext(_options);
            seedContext.Add(protocol);
            seedContext.AddRange(treatments);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            return protocol;
        }

        /*
         * Reads back through a fresh context with query filters disabled, because the
         * point of a soft delete is that the rows still exist while being hidden from
         * normal queries
         */
        private async Task<(TreatmentProtocol Protocol, List<Treatment> Treatments)> LoadAsync(Guid id)
        {
            await using var freshContext = new ClinicalDbContext(_options);
            var protocol = await freshContext.TreatmentProtocols
                .IgnoreQueryFilters()
                .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
            var treatments = await freshContext.Treatments
                .IgnoreQueryFilters()
                .Where(x => x.TreatmentProtocolId == id)
                .ToListAsync(TestContext.Current.CancellationToken);
            return (protocol, treatments);
        }

        private async Task<bool> IsProtocolVisibleAsync(Guid id)
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.TreatmentProtocols
                .AnyAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        }

        private async Task<int> CountVisibleTreatmentsAsync(Guid protocolId)
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.Treatments
                .CountAsync(x => x.TreatmentProtocolId == protocolId,
                    TestContext.Current.CancellationToken);
        }

        private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
        {
            // 1 .NET tick = 100 ns, 1 PostgreSQL microsecond = 10 ticks
            return new DateTimeOffset(value.Ticks - (value.Ticks % 10), value.Offset);
        }

        #endregion

        #region Happy path

        // Boundary values of the enum fields: the lowest and highest defined members
        [Theory]
        [InlineData(Severity.Mild, TreatmentSite.Outpatient)]
        [InlineData(Severity.Severe, TreatmentSite.IntensiveCareUnit)]
        public async Task DeleteTreatmentProtocol_WithSingleTreatment_CascadesSoftDelete_Success(
            Severity severity, TreatmentSite treatmentSite)
        {
            // Boundary: the smallest non-empty cascade has exactly 1 treatment
            var protocol = await SeedProtocolAsync(
                treatmentCount: 1, severity: severity, treatmentSite: treatmentSite);

            var result = await _handler.HandleAsync(
                new DeleteTreatmentProtocolCommand { Id = protocol.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            // Business rule: the protocol is soft deleted, never removed
            var (deleted, treatments) = await LoadAsync(protocol.Id);
            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);
            Assert.Equal(severity, deleted.Severity);
            Assert.Equal(treatmentSite, deleted.TreatmentSite);
            Assert.Equal("2026.1", deleted.Version);
            Assert.Equal("Hiệp hội Lồng ngực Anh (BTS)", deleted.Publisher);
            Assert.True(deleted.IsApplied);

            // Business rule: all associated treatments are soft deleted as well
            var treatment = Assert.Single(treatments);
            Assert.True(treatment.IsDeleted);
            Assert.NotNull(treatment.DeletedAt);

            // Soft-deleted rows must be hidden from normal queries
            Assert.False(await IsProtocolVisibleAsync(protocol.Id));
            Assert.Equal(0, await CountVisibleTreatmentsAsync(protocol.Id));
        }

        [Fact]
        public async Task DeleteTreatmentProtocol_WithoutTreatments_Success()
        {
            // Boundary: 0 associated treatments, one below the minimum cascade size
            var protocol = await SeedProtocolAsync(treatmentCount: 0);

            var result = await _handler.HandleAsync(
                new DeleteTreatmentProtocolCommand { Id = protocol.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            var (deleted, treatments) = await LoadAsync(protocol.Id);
            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);
            Assert.Empty(treatments);

            Assert.False(await IsProtocolVisibleAsync(protocol.Id));
        }

        [Fact]
        public async Task DeleteTreatmentProtocol_WithMultipleTreatments_CascadesSoftDelete_Success()
        {
            /*
             * Boundary: 2 treatments in the target protocol. A second protocol with its
             * own treatment acts as a control, so a blanket update of the treatments
             * table would be detectable
             */
            var target = await SeedProtocolAsync(
                treatmentCount: 2, severity: Severity.Severe,
                treatmentSite: TreatmentSite.IntensiveCareUnit);
            var control = await SeedProtocolAsync(
                treatmentCount: 1, severity: Severity.Mild,
                treatmentSite: TreatmentSite.Outpatient);

            var result = await _handler.HandleAsync(
                new DeleteTreatmentProtocolCommand { Id = target.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            var (deletedTarget, targetTreatments) = await LoadAsync(target.Id);
            Assert.True(deletedTarget.IsDeleted);
            Assert.NotNull(deletedTarget.DeletedAt);
            Assert.Equal(2, targetTreatments.Count);
            Assert.All(targetTreatments, x =>
            {
                Assert.True(x.IsDeleted);
                Assert.NotNull(x.DeletedAt);
            });

            // The control protocol and its treatment must stay untouched
            var (controlAfter, controlTreatments) = await LoadAsync(control.Id);
            Assert.False(controlAfter.IsDeleted);
            Assert.Null(controlAfter.DeletedAt);
            var controlTreatment = Assert.Single(controlTreatments);
            Assert.False(controlTreatment.IsDeleted);
            Assert.Null(controlTreatment.DeletedAt);

            Assert.True(await IsProtocolVisibleAsync(control.Id));
            Assert.Equal(1, await CountVisibleTreatmentsAsync(control.Id));
        }

        [Fact]
        public async Task DeleteTreatmentProtocol_AlreadyDeleted_Fail()
        {
            // A retried delete is served by the same request path: the protocol is now
            // hidden by the query filter, so the second call must report not found and
            // must not move the deletion timestamp again
            var protocol = await SeedProtocolAsync(
                treatmentCount: 1, severity: Severity.Moderate,
                treatmentSite: TreatmentSite.Inpatient);

            var first = await _handler.HandleAsync(
                new DeleteTreatmentProtocolCommand { Id = protocol.Id },
                TestContext.Current.CancellationToken);
            Assert.True(first.IsSuccess());

            var (afterFirst, _) = await LoadAsync(protocol.Id);
            var firstDeletedAt = afterFirst.DeletedAt;

            var second = await _handler.HandleAsync(
                new DeleteTreatmentProtocolCommand { Id = protocol.Id },
                TestContext.Current.CancellationToken);

            Assert.True(second.IsFailure());
            Assert.NotNull(second.Error);
            Assert.Equal(ApplicationStatus.BadRequest, second.StatusCode);

            var (afterSecond, treatments) = await LoadAsync(protocol.Id);
            Assert.True(afterSecond.IsDeleted);
            Assert.Equal(
                TruncateToMicroseconds(firstDeletedAt!.Value),
                TruncateToMicroseconds(afterSecond.DeletedAt!.Value));
            Assert.All(treatments, x => Assert.True(x.IsDeleted));
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task DeleteTreatmentProtocol_ProtocolNotFound_Fail()
        {
            var result = await _handler.HandleAsync(
                new DeleteTreatmentProtocolCommand { Id = Guid.CreateVersion7() },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Nothing must be soft deleted when the target does not exist
            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.TreatmentProtocols
                .IgnoreQueryFilters().CountAsync(TestContext.Current.CancellationToken));
            Assert.Equal(0, await freshContext.Treatments
                .IgnoreQueryFilters().CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteTreatmentProtocol_SoftDeletedProtocol_Fail()
        {
            // A soft-deleted protocol is hidden by the query filter, so deleting it must
            // be rejected just like an unknown protocol. The seeded state is consistent:
            // the protocol and its treatment were soft deleted together
            var protocol = await SeedProtocolAsync(
                treatmentCount: 1, softDeleted: true, severity: Severity.Severe,
                treatmentSite: TreatmentSite.IntensiveCareUnit);

            var result = await _handler.HandleAsync(
                new DeleteTreatmentProtocolCommand { Id = protocol.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The existing soft-delete state must not be touched by the failed call
            var (deleted, treatments) = await LoadAsync(protocol.Id);
            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);
            var treatment = Assert.Single(treatments);
            Assert.True(treatment.IsDeleted);
            Assert.NotNull(treatment.DeletedAt);

            Assert.False(await IsProtocolVisibleAsync(protocol.Id));
        }

        #endregion
    }
}
