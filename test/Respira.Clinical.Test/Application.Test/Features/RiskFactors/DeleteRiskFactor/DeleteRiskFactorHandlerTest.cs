using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.RiskFactors.DeleteRiskFactor;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.RiskFactors.DeleteRiskFactor
{
    public class DeleteRiskFactorHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly DeleteRiskFactorHandler _handler;
        private readonly IDbContext _context;

        public DeleteRiskFactorHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var logger = new Mock<ILogger<DeleteRiskFactorHandler>>().Object;

            // Initialize handler
            _handler = new(_context, logger);
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

        // Read through a context that ignores the query filter, so a soft deleted row
        // can still be inspected after the handler hid it
        private async Task<RiskFactor?> FindIncludingDeletedAsync(Guid id)
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.RiskFactors.IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        }

        #region Happy path

        [Fact]
        public async Task DeleteRiskFactor_Success()
        {
            // Lower boundary: the only risk factor in the table, so the delete leaves
            // zero live rows behind
            var pathogenId = await SeedKlebsiellaAsync();
            var criterionId = await SeedAgeCriterionAsync();
            var target = await SeedRiskFactorAsync(pathogenId, criterionId);

            var result = await _handler.HandleAsync(new DeleteRiskFactorCommand
            {
                Id = target.Id,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            var saved = await FindIncludingDeletedAsync(target.Id);
            Assert.NotNull(saved);

            // Business rule: a delete is a soft delete, the row survives with a
            // deletion timestamp so the history of the risk factor is kept
            Assert.True(saved.IsDeleted);
            Assert.NotNull(saved.DeletedAt);
            var deletedAt = saved.DeletedAt.Value;
            Assert.True(deletedAt >= saved.CreatedAt);

            // The default query filter must now hide the deleted row
            await using var filteredContext = new ClinicalDbContext(_options);
            Assert.False(await filteredContext.RiskFactors
                .AnyAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken));
            Assert.Equal(0, await filteredContext.RiskFactors
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteRiskFactor_OneAmongSeveral_UpperBoundary_Success()
        {
            // Upper boundary: three risk factors, the middle one is removed and the
            // two others must stay live
            var pathogenId = await SeedKlebsiellaAsync();
            var ageCriterionId = await SeedAgeCriterionAsync();
            var hypotensionCriterionId = await SeedHypotensionCriterionAsync();
            var pneumococcusId = await SeedPneumococcusAsync();

            var first = await SeedRiskFactorAsync(pathogenId, ageCriterionId);
            var target = await SeedRiskFactorAsync(pathogenId, hypotensionCriterionId);
            var third = await SeedRiskFactorAsync(pneumococcusId, ageCriterionId);

            var result = await _handler.HandleAsync(new DeleteRiskFactorCommand
            {
                Id = target.Id,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            Assert.True((await FindIncludingDeletedAsync(target.Id))!.IsDeleted);
            Assert.False((await FindIncludingDeletedAsync(first.Id))!.IsDeleted);
            Assert.False((await FindIncludingDeletedAsync(third.Id))!.IsDeleted);

            await using var filteredContext = new ClinicalDbContext(_options);
            Assert.Equal(2, await filteredContext.RiskFactors
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteRiskFactor_SamePathogenDifferentCriterion_Kept_Success()
        {
            /*
             * Business rule: uniqueness covers the pair (pathogen, criterion), so the
             * sibling row of the same pathogen is a separate risk factor and must not
             * be caught by the delete
             */
            var pathogenId = await SeedKlebsiellaAsync();
            var ageCriterionId = await SeedAgeCriterionAsync();
            var hypotensionCriterionId = await SeedHypotensionCriterionAsync();

            var sibling = await SeedRiskFactorAsync(pathogenId, ageCriterionId);
            var target = await SeedRiskFactorAsync(pathogenId, hypotensionCriterionId);

            var result = await _handler.HandleAsync(new DeleteRiskFactorCommand
            {
                Id = target.Id,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            var savedSibling = await FindIncludingDeletedAsync(sibling.Id);
            Assert.NotNull(savedSibling);
            Assert.False(savedSibling.IsDeleted);
            Assert.Null(savedSibling.DeletedAt);
        }

        [Fact]
        public async Task DeleteRiskFactor_ParentsAndControlKept_Success()
        {
            /*
             * Removing the link must never remove what it links: the pathogen, the
             * criterion and a control risk factor built on the same pair under a
             * different pathogen all stay active
             */
            var pathogenId = await SeedKlebsiellaAsync();
            var pneumococcusId = await SeedPneumococcusAsync();
            var ageCriterionId = await SeedAgeCriterionAsync();

            var target = await SeedRiskFactorAsync(pathogenId, ageCriterionId);
            var control = await SeedRiskFactorAsync(pneumococcusId, ageCriterionId);

            var result = await _handler.HandleAsync(new DeleteRiskFactorCommand
            {
                Id = target.Id,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var savedControl = await freshContext.RiskFactors
                .SingleAsync(x => x.Id == control.Id, TestContext.Current.CancellationToken);
            Assert.False(savedControl.IsDeleted);

            Assert.True(await freshContext.Pathogens.AnyAsync(
                x => x.Id == pathogenId, TestContext.Current.CancellationToken));
            Assert.True(await freshContext.Pathogens.AnyAsync(
                x => x.Id == pneumococcusId, TestContext.Current.CancellationToken));
            Assert.True(await freshContext.Criteria.AnyAsync(
                x => x.Id == ageCriterionId, TestContext.Current.CancellationToken));
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task DeleteRiskFactor_UnknownId_Fail()
        {
            var result = await _handler.HandleAsync(new DeleteRiskFactorCommand
            {
                Id = Guid.CreateVersion7(),
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task DeleteRiskFactor_EmptyId_Fail()
        {
            // Boundary: no row can ever match Guid.Empty, so the lookup fails
            var result = await _handler.HandleAsync(new DeleteRiskFactorCommand
            {
                Id = Guid.Empty,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task DeleteRiskFactor_AlreadyDeleted_Fail()
        {
            /*
             * A soft deleted row is hidden by the query filter, so a second delete
             * cannot find it and is rejected instead of overwriting DeletedAt
             */
            var pathogenId = await SeedKlebsiellaAsync();
            var criterionId = await SeedAgeCriterionAsync();
            var target = await SeedRiskFactorAsync(pathogenId, criterionId);

            var first = await _handler.HandleAsync(new DeleteRiskFactorCommand
            {
                Id = target.Id,
            }, TestContext.Current.CancellationToken);
            Assert.True(first.IsSuccess());

            var firstRead = await FindIncludingDeletedAsync(target.Id);
            Assert.NotNull(firstRead);
            var firstDeletedAt = firstRead.DeletedAt;

            var second = await _handler.HandleAsync(new DeleteRiskFactorCommand
            {
                Id = target.Id,
            }, TestContext.Current.CancellationToken);

            Assert.True(second.IsFailure());
            Assert.NotNull(second.Error);
            Assert.Equal(ApplicationStatus.BadRequest, second.StatusCode);

            // The rejected call must not touch the row it could not find
            var secondRead = await FindIncludingDeletedAsync(target.Id);
            Assert.NotNull(secondRead);
            Assert.True(secondRead.IsDeleted);
            Assert.Equal(firstDeletedAt, secondRead.DeletedAt);
        }

        #endregion
    }
}
