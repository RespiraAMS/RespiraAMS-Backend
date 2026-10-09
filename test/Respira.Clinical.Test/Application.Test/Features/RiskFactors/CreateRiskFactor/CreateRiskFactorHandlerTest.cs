using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.RiskFactors.CreateRiskFactor;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.RiskFactors.CreateRiskFactor
{
    public class CreateRiskFactorHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly CreateRiskFactorHandler _handler;
        private readonly IDbContext _context;

        public CreateRiskFactorHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var mapper = new CreateRiskFactorMapper();
            var logger = new Mock<ILogger<CreateRiskFactorHandler>>().Object;

            // Initialize handler
            _handler = new(_context, mapper, logger);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Clear leftover data (children first for FK constraints) so the count
            // assertions are deterministic across runs. IgnoreQueryFilters because
            // soft deleted rows are hidden by the query filter but still occupy the table
            await _context.MetricsRules.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.RiskFactors.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Treatments.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.ClinicalMetrics.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Pathogens.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Criteria.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        private async Task<Guid> SeedPathogenAsync(
            string name, string description, bool isAtypical, bool isDeleted = false)
        {
            var pathogen = new Pathogen
            {
                Name = name,
                Description = description,
                IsAtypical = isAtypical,
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.Pathogens.AddAsync(pathogen, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return pathogen.Id;
        }

        // The formula mirrors the real seed data so the seeded criterion stays a legal
        // boolean criterion (CURB-65 age >= 65, hypotension SBP < 90 mmHg)
        private async Task<Guid> SeedCriterionAsync(
            string name, string code, decimal threshold, ExpressionOperator op, bool isDeleted = false)
        {
            var criterion = new Criterion(name, new BinaryFormula(
                new VariableFormula(new VariableRef(Guid.CreateVersion7(), code, ClinicalValueType.Numeric)),
                new NumericConstantFormula(threshold),
                op))
            {
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.Criteria.AddAsync(criterion, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return criterion.Id;
        }

        private async Task<Guid> SeedPathogenAsync() => await SeedPathogenAsync(
            "Klebsiella pneumoniae",
            "Gram-negative bacillus causing hospital-acquired pneumonia",
            isAtypical: true);

        private async Task<Guid> SeedAgeCriterionAsync() => await SeedCriterionAsync(
            "Tuổi >= 65", "AGE", 65, ExpressionOperator.GTE);

        #region Happy path

        [Fact]
        public async Task CreateRiskFactor_Success()
        {
            // Lower boundary of the uniqueness check: 0 rows share this pair
            var pathogenId = await SeedPathogenAsync();
            var criterionId = await SeedAgeCriterionAsync();

            var result = await _handler.HandleAsync(new CreateRiskFactorCommand
            {
                PathogenId = pathogenId,
                CriterionId = criterionId,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.NotEqual(Guid.Empty, result.Data.Id);

            // Verify through a fresh context so the change tracker cannot mask a failed commit
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.RiskFactors
                .SingleAsync(x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);

            Assert.Equal(pathogenId, saved.PathogenId);
            Assert.Equal(criterionId, saved.CriterionId);
            Assert.False(saved.IsDeleted);
            Assert.Null(saved.DeletedAt);
        }

        [Fact]
        public async Task CreateRiskFactor_SamePathogenDifferentCriterion_Success()
        {
            /*
             * Business rule: uniqueness covers the pair (pathogen, criterion), not the
             * pathogen alone. One pathogen may put a patient at risk through several
             * independent criteria
             */
            var pathogenId = await SeedPathogenAsync();
            var ageCriterionId = await SeedAgeCriterionAsync();
            var hypotensionCriterionId = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", "SBP", 90, ExpressionOperator.LT);

            var result = await _handler.HandleAsync(new CreateRiskFactorCommand
            {
                PathogenId = pathogenId,
                CriterionId = ageCriterionId,
            }, TestContext.Current.CancellationToken);
            var second = await _handler.HandleAsync(new CreateRiskFactorCommand
            {
                PathogenId = pathogenId,
                CriterionId = hypotensionCriterionId,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.True(second.IsSuccess());
            Assert.Equal(ApplicationStatus.Created, second.StatusCode);
            Assert.NotNull(result.Data);
            Assert.NotNull(second.Data);
            Assert.NotEqual(result.Data.Id, second.Data.Id);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(2, await freshContext.RiskFactors
                .CountAsync(x => x.PathogenId == pathogenId, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateRiskFactor_DifferentPathogenSameCriterion_Success()
        {
            // The other half of the boundary: the criterion may link to several pathogens
            var pathogenOne = await SeedPathogenAsync();
            var pathogenTwo = await SeedPathogenAsync(
                "Streptococcus pneumoniae",
                "Gram-positive diplococcus, the most common community acquired pathogen",
                isAtypical: false);
            var criterionId = await SeedAgeCriterionAsync();

            var first = await _handler.HandleAsync(new CreateRiskFactorCommand
            {
                PathogenId = pathogenOne,
                CriterionId = criterionId,
            }, TestContext.Current.CancellationToken);
            var second = await _handler.HandleAsync(new CreateRiskFactorCommand
            {
                PathogenId = pathogenTwo,
                CriterionId = criterionId,
            }, TestContext.Current.CancellationToken);

            Assert.True(first.IsSuccess());
            Assert.True(second.IsSuccess());
            Assert.NotNull(first.Data);
            Assert.NotNull(second.Data);
            Assert.NotEqual(first.Data.Id, second.Data.Id);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(2, await freshContext.RiskFactors
                .CountAsync(x => x.CriterionId == criterionId, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateRiskFactor_SoftDeletedPairCanBeRecreated_Success()
        {
            /*
             * Uniqueness is enforced at the application level because soft delete
             * replaces a DB UNIQUE index, so the uniqueness query only sees live rows
             * and the same pair can be created again after a delete
             */
            var pathogenId = await SeedPathogenAsync();
            var criterionId = await SeedAgeCriterionAsync();
            await _context.RiskFactors.AddAsync(new RiskFactor
            {
                PathogenId = pathogenId,
                CriterionId = criterionId,
                IsDeleted = true,
                DeletedAt = DateTimeOffset.UtcNow,
            }, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(new CreateRiskFactorCommand
            {
                PathogenId = pathogenId,
                CriterionId = criterionId,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);

            await using var freshContext = new ClinicalDbContext(_options);
            var alive = await freshContext.RiskFactors
                .SingleAsync(x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);
            Assert.False(alive.IsDeleted);

            Assert.Equal(2, await freshContext.RiskFactors.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task CreateRiskFactor_DuplicatePair_Fail()
        {
            // Smallest non zero boundary: exactly 1 live row already holds this pair
            var pathogenId = await SeedPathogenAsync();
            var criterionId = await SeedAgeCriterionAsync();
            await _context.RiskFactors.AddAsync(new RiskFactor
            {
                PathogenId = pathogenId,
                CriterionId = criterionId,
            }, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(new CreateRiskFactorCommand
            {
                PathogenId = pathogenId,
                CriterionId = criterionId,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            // The duplicate must not be persisted
            Assert.Equal(1, await _context.RiskFactors.CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateRiskFactor_UnknownPathogen_Fail()
        {
            var criterionId = await SeedAgeCriterionAsync();

            var result = await _handler.HandleAsync(new CreateRiskFactorCommand
            {
                PathogenId = Guid.CreateVersion7(),
                CriterionId = criterionId,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            Assert.Equal(0, await _context.RiskFactors.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateRiskFactor_UnknownCriterion_Fail()
        {
            var pathogenId = await SeedPathogenAsync();

            var result = await _handler.HandleAsync(new CreateRiskFactorCommand
            {
                PathogenId = pathogenId,
                CriterionId = Guid.CreateVersion7(),
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            Assert.Equal(0, await _context.RiskFactors.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateRiskFactor_SoftDeletedPathogen_Fail()
        {
            // A soft-deleted pathogen is hidden by the query filter, so referencing it
            // must be rejected just like an unknown pathogen
            var pathogenId = await SeedPathogenAsync(
                "Pseudomonas aeruginosa",
                "Gram-negative opportunist, often multidrug resistant",
                isAtypical: false,
                isDeleted: true);
            var criterionId = await SeedAgeCriterionAsync();

            var result = await _handler.HandleAsync(new CreateRiskFactorCommand
            {
                PathogenId = pathogenId,
                CriterionId = criterionId,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            Assert.Equal(0, await _context.RiskFactors.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateRiskFactor_SoftDeletedCriterion_Fail()
        {
            // A soft-deleted criterion is hidden by the query filter, so referencing it
            // must be rejected just like an unknown criterion
            var pathogenId = await SeedPathogenAsync();
            var criterionId = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", "SBP", 90, ExpressionOperator.LT, isDeleted: true);

            var result = await _handler.HandleAsync(new CreateRiskFactorCommand
            {
                PathogenId = pathogenId,
                CriterionId = criterionId,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            Assert.Equal(0, await _context.RiskFactors.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        #endregion
    }
}
