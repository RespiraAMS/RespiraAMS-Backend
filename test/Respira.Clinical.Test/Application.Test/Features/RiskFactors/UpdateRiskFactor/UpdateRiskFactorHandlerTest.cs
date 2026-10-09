using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.RiskFactors.UpdateRiskFactor;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.RiskFactors.UpdateRiskFactor
{
    public class UpdateRiskFactorHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly UpdateRiskFactorHandler _handler;
        private readonly IDbContext _context;

        // A fixed old timestamp makes the "moved forward" assertion deterministic
        private static readonly DateTimeOffset Stale = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public UpdateRiskFactorHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var mapper = new UpdateRiskFactorMapper();
            var logger = new Mock<ILogger<UpdateRiskFactorHandler>>().Object;

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

        private async Task<RiskFactor> SeedRiskFactorAsync(
            Guid pathogenId, Guid criterionId, bool isDeleted = false)
        {
            var factor = new RiskFactor
            {
                PathogenId = pathogenId,
                CriterionId = criterionId,
                UpdatedAt = Stale,
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.RiskFactors.AddAsync(factor, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return factor;
        }

        private async Task<Guid> SeedKlebsiellaAsync() => await SeedPathogenAsync(
            "Klebsiella pneumoniae",
            "Gram-negative bacillus causing hospital-acquired pneumonia",
            isAtypical: true);

        private async Task<Guid> SeedPneumococcusAsync() => await SeedPathogenAsync(
            "Streptococcus pneumoniae",
            "Gram-positive diplococcus, the most common community acquired pathogen",
            isAtypical: false);

        private async Task<Guid> SeedAgeCriterionAsync() => await SeedCriterionAsync(
            "Tuổi >= 65", "AGE", 65, ExpressionOperator.GTE);

        private async Task<Guid> SeedHypotensionCriterionAsync() => await SeedCriterionAsync(
            "Huyết áp tâm thu < 90 mmHg", "SBP", 90, ExpressionOperator.LT);

        private async Task<RiskFactor?> FindIncludingDeletedAsync(Guid id)
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.RiskFactors.IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        }

        private static UpdateRiskFactorCommand Command(Guid id, Guid pathogenId, Guid criterionId) => new()
        {
            Id = id,
            PathogenId = pathogenId,
            CriterionId = criterionId,
        };

        #region Happy path

        [Fact]
        public async Task UpdateRiskFactor_Success()
        {
            // Full re-point: both foreign keys move to a different pathogen and a
            // different criterion
            var pathogen = await SeedKlebsiellaAsync();
            var criterion = await SeedAgeCriterionAsync();
            var target = await SeedRiskFactorAsync(pathogen, criterion);

            var newPathogen = await SeedPneumococcusAsync();
            var newCriterion = await SeedHypotensionCriterionAsync();

            var result = await _handler.HandleAsync(
                Command(target.Id, newPathogen, newCriterion), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            // Verify through a fresh context so the change tracker cannot mask a failed commit
            var saved = await FindIncludingDeletedAsync(target.Id);
            Assert.NotNull(saved);
            Assert.Equal(newPathogen, saved.PathogenId);
            Assert.Equal(newCriterion, saved.CriterionId);

            // The update must move the timestamp and keep the identity and the state
            Assert.True(
                saved.UpdatedAt > Stale,
                $"UpdatedAt {saved.UpdatedAt} should move past the stale value {Stale}");
            Assert.Equal(target.Id, saved.Id);
            Assert.False(saved.IsDeleted);
            Assert.Null(saved.DeletedAt);
        }

        [Fact]
        public async Task UpdateRiskFactor_SamePairUpdate_Success()
        {
            /*
             * Boundary of the uniqueness rule: exactly 1 row already holds this pair and
             * it is the row being updated. The x.Id != command.Id guard keeps a no-op
             * update of its own row legal
             */
            var pathogen = await SeedKlebsiellaAsync();
            var criterion = await SeedAgeCriterionAsync();
            var target = await SeedRiskFactorAsync(pathogen, criterion);

            var result = await _handler.HandleAsync(
                Command(target.Id, pathogen, criterion), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            var saved = await FindIncludingDeletedAsync(target.Id);
            Assert.NotNull(saved);
            Assert.Equal(pathogen, saved.PathogenId);
            Assert.Equal(criterion, saved.CriterionId);
            Assert.True(saved.UpdatedAt > Stale);

            Assert.Equal(1, await _context.RiskFactors
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task UpdateRiskFactor_OnlyPathogenChanged_Success()
        {
            // One half of the pair moves while the criterion stays where it is
            var pathogen = await SeedKlebsiellaAsync();
            var newPathogen = await SeedPneumococcusAsync();
            var criterion = await SeedAgeCriterionAsync();
            var target = await SeedRiskFactorAsync(pathogen, criterion);

            var result = await _handler.HandleAsync(
                Command(target.Id, newPathogen, criterion), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            var saved = await FindIncludingDeletedAsync(target.Id);
            Assert.NotNull(saved);
            Assert.Equal(newPathogen, saved.PathogenId);
            Assert.Equal(criterion, saved.CriterionId);
        }

        [Fact]
        public async Task UpdateRiskFactor_OnlyCriterionChanged_Success()
        {
            // The other half of the pair moves while the pathogen stays where it is
            var pathogen = await SeedKlebsiellaAsync();
            var criterion = await SeedAgeCriterionAsync();
            var newCriterion = await SeedHypotensionCriterionAsync();
            var target = await SeedRiskFactorAsync(pathogen, criterion);

            var result = await _handler.HandleAsync(
                Command(target.Id, pathogen, newCriterion), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            var saved = await FindIncludingDeletedAsync(target.Id);
            Assert.NotNull(saved);
            Assert.Equal(pathogen, saved.PathogenId);
            Assert.Equal(newCriterion, saved.CriterionId);
        }

        [Fact]
        public async Task UpdateRiskFactor_OtherRowsAndParentsUntouched_Success()
        {
            /*
             * Repointing one risk factor must not disturb the control row built on the
             * other pair, and must never touch the pathogen or the criterion it links
             */
            var pathogen = await SeedKlebsiellaAsync();
            var pneumococcus = await SeedPneumococcusAsync();
            var age = await SeedAgeCriterionAsync();
            var hypotension = await SeedHypotensionCriterionAsync();

            var target = await SeedRiskFactorAsync(pathogen, age);
            var control = await SeedRiskFactorAsync(pneumococcus, hypotension);

            var result = await _handler.HandleAsync(
                Command(target.Id, pneumococcus, age), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            var savedControl = await FindIncludingDeletedAsync(control.Id);
            Assert.NotNull(savedControl);
            Assert.Equal(pneumococcus, savedControl.PathogenId);
            Assert.Equal(hypotension, savedControl.CriterionId);
            Assert.Equal(Stale, savedControl.UpdatedAt);
            Assert.False(savedControl.IsDeleted);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(2, await freshContext.RiskFactors
                .CountAsync(TestContext.Current.CancellationToken));
            Assert.True(await freshContext.Pathogens
                .AnyAsync(x => x.Id == pathogen, TestContext.Current.CancellationToken));
            Assert.True(await freshContext.Criteria
                .AnyAsync(x => x.Id == age, TestContext.Current.CancellationToken));
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task UpdateRiskFactor_UnknownId_Fail()
        {
            var pathogen = await SeedKlebsiellaAsync();
            var criterion = await SeedAgeCriterionAsync();

            var result = await _handler.HandleAsync(
                Command(Guid.CreateVersion7(), pathogen, criterion), TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task UpdateRiskFactor_EmptyId_Fail()
        {
            // Boundary: no row can ever match Guid.Empty, so the lookup fails first
            var pathogen = await SeedKlebsiellaAsync();
            var criterion = await SeedAgeCriterionAsync();

            var result = await _handler.HandleAsync(
                Command(Guid.Empty, pathogen, criterion), TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task UpdateRiskFactor_SoftDeletedTarget_Fail()
        {
            // A soft deleted risk factor is hidden by the query filter, so it behaves
            // exactly like an unknown id
            var pathogen = await SeedKlebsiellaAsync();
            var criterion = await SeedAgeCriterionAsync();
            var target = await SeedRiskFactorAsync(pathogen, criterion, isDeleted: true);

            var result = await _handler.HandleAsync(
                Command(target.Id, pathogen, criterion), TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            var saved = await FindIncludingDeletedAsync(target.Id);
            Assert.NotNull(saved);
            Assert.Equal(Stale, saved.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRiskFactor_UnknownPathogen_Fail()
        {
            var originalPathogen = await SeedKlebsiellaAsync();
            var criterion = await SeedAgeCriterionAsync();
            var target = await SeedRiskFactorAsync(originalPathogen, criterion);

            var result = await _handler.HandleAsync(
                Command(target.Id, Guid.CreateVersion7(), criterion), TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The rejected update must leave the row pointing at the original pair
            var saved = await FindIncludingDeletedAsync(target.Id);
            Assert.NotNull(saved);
            Assert.Equal(originalPathogen, saved.PathogenId);
            Assert.Equal(criterion, saved.CriterionId);
            Assert.Equal(Stale, saved.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRiskFactor_UnknownCriterion_Fail()
        {
            var pathogen = await SeedKlebsiellaAsync();
            var originalCriterion = await SeedAgeCriterionAsync();
            var target = await SeedRiskFactorAsync(pathogen, originalCriterion);

            var result = await _handler.HandleAsync(
                Command(target.Id, pathogen, Guid.CreateVersion7()), TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The rejected update must leave the row pointing at the original pair
            var saved = await FindIncludingDeletedAsync(target.Id);
            Assert.NotNull(saved);
            Assert.Equal(pathogen, saved.PathogenId);
            Assert.Equal(originalCriterion, saved.CriterionId);
            Assert.Equal(Stale, saved.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRiskFactor_SoftDeletedPathogen_Fail()
        {
            // A soft deleted pathogen is hidden by the query filter, so referencing it
            // must be rejected just like an unknown pathogen
            var pathogen = await SeedKlebsiellaAsync();
            var deletedPathogen = await SeedPathogenAsync(
                "Pseudomonas aeruginosa",
                "Gram-negative opportunist, often multidrug resistant",
                isAtypical: false,
                isDeleted: true);
            var criterion = await SeedAgeCriterionAsync();
            var target = await SeedRiskFactorAsync(pathogen, criterion);

            var result = await _handler.HandleAsync(
                Command(target.Id, deletedPathogen, criterion), TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            var saved = await FindIncludingDeletedAsync(target.Id);
            Assert.NotNull(saved);
            Assert.Equal(pathogen, saved.PathogenId);
            Assert.Equal(Stale, saved.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRiskFactor_SoftDeletedCriterion_Fail()
        {
            // A soft deleted criterion is hidden by the query filter, so referencing it
            // must be rejected just like an unknown criterion
            var pathogen = await SeedKlebsiellaAsync();
            var deletedCriterion = await SeedCriterionAsync(
                "Huyết áp tâm thu < 90 mmHg", "SBP", 90, ExpressionOperator.LT, isDeleted: true);
            var criterion = await SeedAgeCriterionAsync();
            var target = await SeedRiskFactorAsync(pathogen, criterion);

            var result = await _handler.HandleAsync(
                Command(target.Id, pathogen, deletedCriterion), TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            var saved = await FindIncludingDeletedAsync(target.Id);
            Assert.NotNull(saved);
            Assert.Equal(criterion, saved.CriterionId);
            Assert.Equal(Stale, saved.UpdatedAt);
        }

        [Fact]
        public async Task UpdateRiskFactor_DuplicatePair_Fail()
        {
            // Smallest non zero boundary of the uniqueness rule: exactly 1 other row
            // already holds the requested pair
            var pathogen = await SeedKlebsiellaAsync();
            var pneumococcus = await SeedPneumococcusAsync();
            var age = await SeedAgeCriterionAsync();
            var hypotension = await SeedHypotensionCriterionAsync();

            var target = await SeedRiskFactorAsync(pathogen, age);
            var holder = await SeedRiskFactorAsync(pneumococcus, hypotension);

            var result = await _handler.HandleAsync(
                Command(target.Id, pneumococcus, hypotension), TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Neither row may change
            var savedTarget = await FindIncludingDeletedAsync(target.Id);
            Assert.NotNull(savedTarget);
            Assert.Equal(pathogen, savedTarget.PathogenId);
            Assert.Equal(age, savedTarget.CriterionId);
            Assert.Equal(Stale, savedTarget.UpdatedAt);

            var savedHolder = await FindIncludingDeletedAsync(holder.Id);
            Assert.NotNull(savedHolder);
            Assert.Equal(pneumococcus, savedHolder.PathogenId);
            Assert.Equal(hypotension, savedHolder.CriterionId);
            Assert.Equal(Stale, savedHolder.UpdatedAt);
        }

        #endregion
    }
}
