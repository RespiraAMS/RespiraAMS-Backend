using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.ClinicalMetrics.UpdateClinicalMetric;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.ClinicalMetrics.UpdateClinicalMetric
{
    public class UpdateClinicalMetricHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private const string Curb65Code = "CURB-65";
        private const string PsiCode = "PSI";
        private const string IdsaAtsCode = "IDSA/ATS";

        // Metrics of the real seed data (seed-data.jsonc) plus CRB-65, the CURB-65
        // variant without the blood urea nitrogen component used by the diagnose service
        private static readonly (string Name, string Code, string Description, string Version, string Publisher) Curb65 =
            ("Severity metrics CURB-65", Curb65Code, "CURB-65 metrics to assess severity of a patient",
                "2002-2003", "Hiệp hội Lồng ngực Anh (BTS)");

        private static readonly (string Name, string Code, string Description, string Version, string Publisher) Psi =
            ("Pneumonia Severity Index (PSI)", PsiCode, "PSI metrics to assess severity of a patient with more detail than CURB-65",
                "1997", "Đội ngũ Nghiên cứu Kết quả Điều trị Viêm phổi (PORT - Pneumonia Outcomes Research Team)");

        private static readonly (string Name, string Code, string Description, string Version, string Publisher) IdsaAts =
            ("ICU hospitalization criteria by IDSA/ATS", IdsaAtsCode, "IDSA/ATS metrics to assess whether a severe patient need ICU or not",
                "2026", "Hiệp hội Bệnh nhiễm trùng Mỹ (IDSA) và Hiệp hội Lồng ngực Mỹ (ATS)");

        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly UpdateClinicalMetricHandler _handler;
        private readonly IDbContext _context;

        public UpdateClinicalMetricHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var mapper = new UpdateClinicalMetricMapper();
            var logger = new Mock<ILogger<UpdateClinicalMetricHandler>>().Object;

            // Initialize handler
            _handler = new(_context, mapper, logger);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Clear leftover data (rules first for FK constraints) so the count
            // assertions are deterministic across runs. IgnoreQueryFilters because soft
            // deleted rows are hidden by the query filter but still occupy the table
            await _context.MetricRules.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.ClinicalMetrics.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        #region Happy path

        [Fact]
        public async Task UpdateClinicalMetric_Success()
        {
            /*
             * Boundary: the metric keeps its own code, so the uniqueness rule must
             * exclude the metric being updated from its own check
             */
            var metricId = await SeedMetricAsync(Curb65);
            var psiId = await SeedMetricAsync(Psi);

            var command = CreateCommand(Curb65) with
            {
                Id = metricId,
                Name = "Severity metrics CURB-65 (BTS guideline)",
                Description = "CURB-65 metrics to assess severity of a patient, per the BTS guideline",
                Version = "2009",
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            // Verify through a fresh context so the change tracker of the saving context
            // cannot mask whether the row was truly committed
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalMetrics.SingleAsync(
                x => x.Id == metricId, TestContext.Current.CancellationToken);

            Assert.Equal(command.Name, saved.Name);
            Assert.Equal(Curb65Code, saved.Code);
            Assert.Equal(command.Description, saved.Description);
            Assert.Equal("2009", saved.Version);
            Assert.Equal(Curb65.Publisher, saved.Publisher);

            // Business rule: the update never deletes nor replaces the metric itself
            Assert.Equal(metricId, saved.Id);
            Assert.False(saved.IsDeleted);
            Assert.Null(saved.DeletedAt);
            Assert.Empty(saved.Rules);

            // Business rule: a metric stays in use unless the command says otherwise
            Assert.True(saved.IsApplied);

            // The unrelated metric of the same table must stay untouched
            var untouched = await freshContext.ClinicalMetrics.SingleAsync(
                x => x.Id == psiId, TestContext.Current.CancellationToken);
            Assert.Equal(Psi.Name, untouched.Name);
            Assert.Equal(PsiCode, untouched.Code);
            Assert.True(untouched.IsApplied);
            Assert.Equal(2, await freshContext.ClinicalMetrics
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task UpdateClinicalMetric_SwitchMetricOff_Success()
        {
            /*
             * Business rule: the update is the way a metric leaves the in-use set. Once
             * off, its code stops being reserved for any other feature
             */
            var metricId = await SeedMetricAsync(IdsaAts);

            var command = CreateCommand(IdsaAts) with { Id = metricId, IsApplied = false };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalMetrics.SingleAsync(
                x => x.Id == metricId, TestContext.Current.CancellationToken);

            Assert.False(saved.IsApplied);
            // Every other field keeps the payload of the command
            Assert.Equal(IdsaAts.Name, saved.Name);
            Assert.Equal(IdsaAtsCode, saved.Code);
            Assert.Equal(IdsaAts.Version, saved.Version);
            Assert.False(saved.IsDeleted);
        }

        [Fact]
        public async Task UpdateClinicalMetric_SwitchMetricOn_Success()
        {
            // The other direction: a drafted metric enters the in-use set
            var metricId = await SeedMetricAsync(IdsaAts, isApplied: false);

            var command = CreateCommand(IdsaAts) with { Id = metricId, IsApplied = true };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalMetrics.SingleAsync(
                x => x.Id == metricId, TestContext.Current.CancellationToken);

            Assert.True(saved.IsApplied);
            Assert.Equal(IdsaAtsCode, saved.Code);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task UpdateClinicalMetric_UnknownId_Fail()
        {
            var metricId = await SeedMetricAsync(Curb65);
            var unknownId = Guid.CreateVersion7();

            var command = CreateCommand(Curb65) with
            {
                Id = unknownId,
                Name = "Severity metrics of an unknown metric",
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The real metric must stay untouched
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalMetrics.SingleAsync(
                x => x.Id == metricId, TestContext.Current.CancellationToken);
            Assert.Equal(Curb65.Name, saved.Name);
            Assert.Equal(1, await freshContext.ClinicalMetrics.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task UpdateClinicalMetric_EmptyId_Fail()
        {
            // Boundary: Guid.Empty can never match a stored metric
            var metricId = await SeedMetricAsync(Curb65);

            var command = CreateCommand(Curb65) with
            {
                Id = Guid.Empty,
                Name = "Severity metrics CURB-65 (BTS guideline)",
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalMetrics.SingleAsync(
                x => x.Id == metricId, TestContext.Current.CancellationToken);
            Assert.Equal(Curb65.Name, saved.Name);
        }

        [Fact]
        public async Task UpdateClinicalMetric_SoftDeletedMetric_Fail()
        {
            // The !IsDeleted query filter hides soft deleted rows, so updating one is
            // reported as a missing metric
            var metricId = await SeedMetricAsync(Curb65, isDeleted: true);

            var command = CreateCommand(Curb65) with
            {
                Id = metricId,
                Name = "Severity metrics CURB-65 renamed",
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalMetrics.IgnoreQueryFilters().SingleAsync(
                x => x.Id == metricId, TestContext.Current.CancellationToken);
            Assert.Equal(Curb65.Name, saved.Name);
            Assert.True(saved.IsDeleted);
        }

        [Fact]
        public async Task UpdateClinicalMetric_MappingFailure_Fail()
        {
            /*
             * The real mapper cannot fail, so the handler mapping failure branch is
             * covered by substituting a mapper that reports a failure: nothing may be
             * saved in that case
             */
            var metricId = await SeedMetricAsync(Curb65);

            var mapper = new Mock<IUpdateMapper<ClinicalMetric, UpdateClinicalMetricCommand>>();
            mapper.Setup(x => x.MapModel(It.IsAny<ClinicalMetric>(), It.IsAny<UpdateClinicalMetricCommand>()))
                .Returns(Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Unable to map the clinical metric")));
            var handler = new UpdateClinicalMetricHandler(
                _context,
                mapper.Object,
                new Mock<ILogger<UpdateClinicalMetricHandler>>().Object);

            var command = CreateCommand(Psi) with { Id = metricId };

            var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The mapping runs after the metric is loaded but before the save, so the
            // database must still hold the original payload
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalMetrics.SingleAsync(
                x => x.Id == metricId, TestContext.Current.CancellationToken);
            Assert.Equal(Curb65Code, saved.Code);
            Assert.Equal(Curb65.Name, saved.Name);
            Assert.True(saved.IsApplied);
        }

        #endregion

        private async Task<Guid> SeedMetricAsync(
            (string Name, string Code, string Description, string Version, string Publisher) metric,
            bool isApplied = true,
            bool isDeleted = false)
        {
            var entity = new ClinicalMetric(
                metric.Name,
                metric.Code,
                metric.Description,
                metric.Version,
                metric.Publisher,
                isApplied,
                [])
            {
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };

            await _context.ClinicalMetrics.AddAsync(entity, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return entity.Id;
        }

        private static UpdateClinicalMetricCommand CreateCommand(
            (string Name, string Code, string Description, string Version, string Publisher) metric)
        {
            return new UpdateClinicalMetricCommand
            {
                Id = Guid.CreateVersion7(),
                Name = metric.Name,
                Description = metric.Description,
                Version = metric.Version,
                Publisher = metric.Publisher,
                IsApplied = true,
            };
        }
    }
}
