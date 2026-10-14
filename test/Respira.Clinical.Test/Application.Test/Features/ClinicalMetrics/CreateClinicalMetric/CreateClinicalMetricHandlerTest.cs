using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.ClinicalMetrics.CreateClinicalMetric;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.ClinicalMetrics.CreateClinicalMetric
{
    public class CreateClinicalMetricHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private const string Curb65Code = "CURB-65";
        private const string PsiCode = "PSI";
        private const string IdsaAtsCode = "IDSA/ATS";

        // The three metrics shipped by the real seed data (seed-data.jsonc)
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
        private readonly CreateClinicalMetricHandler _handler;
        private readonly IDbContext _context;

        public CreateClinicalMetricHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var mapper = new CreateClinicalMetricMapper();
            var logger = new Mock<ILogger<CreateClinicalMetricHandler>>().Object;

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
        public async Task CreateClinicalMetric_Success()
        {
            var command = CreateCommand(Curb65);

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.NotEqual(Guid.Empty, result.Data.Id);

            // Verify through a fresh context so the change tracker of the saving context
            // cannot mask whether the row was truly committed
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalMetrics.SingleAsync(
                x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);

            Assert.Equal(Curb65.Name, saved.Name);
            Assert.Equal(Curb65.Code, saved.Code);
            Assert.Equal(Curb65.Description, saved.Description);
            Assert.Equal(Curb65.Version, saved.Version);
            Assert.Equal(Curb65.Publisher, saved.Publisher);

            // Business rule: a metric being created is always applied (in use)
            Assert.True(saved.IsApplied);

            // Business rule: the feature creates the metric without any rule, so it
            // belongs to neither the scoring nor the major/minor system yet
            Assert.Empty(saved.Rules);
            Assert.False(saved.IsScoringMetric);
            Assert.False(saved.IsMajorMinorMetric);

            Assert.False(saved.IsDeleted);
            Assert.Null(saved.DeletedAt);
        }

        [Fact]
        public async Task CreateClinicalMetric_ExistingDifferentCode_Success()
        {
            // Boundary: no existing row holds the new code, so an in-use metric of
            // another code must not block the creation
            var seededId = await SeedMetricAsync(Curb65);

            var result = await _handler.HandleAsync(CreateCommand(Psi), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.NotEqual(Guid.Empty, result.Data.Id);
            Assert.NotEqual(seededId, result.Data.Id);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(2, await freshContext.ClinicalMetrics
                .CountAsync(TestContext.Current.CancellationToken));

            var saved = await freshContext.ClinicalMetrics.SingleAsync(
                x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);
            Assert.Equal(PsiCode, saved.Code);
            Assert.True(saved.IsApplied);
        }

        [Fact]
        public async Task CreateClinicalMetric_SameCodeNotInUse_Success()
        {
            /*
             * Business rule: uniqueness only covers metrics that are in use, a metric
             * switched off (isApplied = false, e.g. through an update) no longer
             * reserves its code
             */
            var retiredId = await SeedMetricAsync(Psi, isApplied: false);

            var result = await _handler.HandleAsync(CreateCommand(Psi), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.NotEqual(retiredId, result.Data.Id);

            await using var freshContext = new ClinicalDbContext(_options);
            var psiMetrics = await freshContext.ClinicalMetrics
                .Where(x => x.Code == PsiCode)
                .ToListAsync(TestContext.Current.CancellationToken);

            Assert.Equal(2, psiMetrics.Count);
            Assert.True(psiMetrics.Single(x => x.Id == result.Data.Id).IsApplied);
            Assert.False(psiMetrics.Single(x => x.Id == retiredId).IsApplied);
        }

        [Fact]
        public async Task CreateClinicalMetric_SameCodeSoftDeleted_Success()
        {
            // A soft deleted metric is hidden by the query filter, so its code is free again
            await SeedMetricAsync(Curb65, isDeleted: true);

            var result = await _handler.HandleAsync(CreateCommand(Curb65), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            // Only the freshly created metric counts as live, the soft deleted one
            // still occupies the table
            Assert.Equal(1, await freshContext.ClinicalMetrics
                .CountAsync(TestContext.Current.CancellationToken));
            Assert.Equal(2, await freshContext.ClinicalMetrics.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        #endregion

        #region Fail path

        [Theory]
        [InlineData(Curb65Code)]
        [InlineData(PsiCode)]
        [InlineData(IdsaAtsCode)]
        public async Task CreateClinicalMetric_DuplicateInUseCode_Fail(string code)
        {
            /*
             * Boundary: exactly one live metric that is in use already holds this code,
             * the smallest count that triggers the uniqueness rule. Every other field
             * of the new metric differs, so the decision is taken on the code alone
             */
            var seededId = await SeedMetricAsync(MetricFor(code));
            var command = CreateCommand(MetricFor(code)) with
            {
                Name = $"Updated severity metrics {code}",
                Version = "2026",
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            // The rejected metric must not be persisted
            await using var freshContext = new ClinicalDbContext(_options);
            var metrics = await freshContext.ClinicalMetrics.IgnoreQueryFilters()
                .ToListAsync(TestContext.Current.CancellationToken);
            _ = Assert.Single(metrics);
            Assert.Equal(seededId, metrics[0].Id);
            Assert.True(metrics[0].IsApplied);
        }

        [Fact]
        public async Task CreateClinicalMetric_MappingFailure_Fail()
        {
            /*
             * The real mapper cannot fail (it only builds a rule-less metric), so the
             * mapping failure branch of the handler is covered by substituting a mapper
             * that reports a failure: whatever the reason, nothing may reach the database
             */
            var mapper = new Mock<ICreateMapper<CreateClinicalMetricCommand, ClinicalMetric>>();
            mapper.Setup(x => x.ToModel(It.IsAny<CreateClinicalMetricCommand>()))
                .Returns(Result<ClinicalMetric>.Failure(
                    new Error(ApplicationStatus.BadRequest, "Unable to map the clinical metric")));
            var handler = new CreateClinicalMetricHandler(
                _context,
                mapper.Object,
                new Mock<ILogger<CreateClinicalMetricHandler>>().Object);

            var result = await handler.HandleAsync(CreateCommand(Curb65), TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.ClinicalMetrics.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
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

        private static (string Name, string Code, string Description, string Version, string Publisher) MetricFor(
            string code) => code switch
            {
                Curb65Code => Curb65,
                PsiCode => Psi,
                IdsaAtsCode => IdsaAts,
                _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown clinical metric"),
            };

        private static CreateClinicalMetricCommand CreateCommand(
            (string Name, string Code, string Description, string Version, string Publisher) metric)
        {
            return new CreateClinicalMetricCommand
            {
                Name = metric.Name,
                Code = metric.Code,
                Description = metric.Description,
                Version = metric.Version,
                Publisher = metric.Publisher,
            };
        }
    }
}
