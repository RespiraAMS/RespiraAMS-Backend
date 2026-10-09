using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.RiskFactors.GetRiskFactorById;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.RiskFactors.GetRiskFactorById
{
    public class GetRiskFactorByIdHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly GetRiskFactorByIdHandler _handler;
        private readonly IDbContext _context;

        public GetRiskFactorByIdHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>()
                .UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);

            // Initialize handler
            _handler = new(_context);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Clear leftover data (children first for FK constraints) so the not found
            // assertions are deterministic across runs. IgnoreQueryFilters because soft
            // deleted rows are hidden by the query filter but still occupy the table
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

        private async Task<Guid> SeedPathogenAsync(string name, string description, bool isAtypical)
        {
            var pathogen = new Pathogen
            {
                Name = name,
                Description = description,
                IsAtypical = isAtypical,
            };
            await _context.Pathogens.AddAsync(pathogen, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return pathogen.Id;
        }

        // The formula mirrors the real seed data so the seeded criterion stays a legal
        // boolean criterion (CURB-65 age >= 65, hypotension SBP < 90 mmHg)
        private async Task<Guid> SeedCriterionAsync(
            string name, Formula formula, bool isDeleted = false)
        {
            var criterion = new Criterion(name, formula)
            {
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.Criteria.AddAsync(criterion, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return criterion.Id;
        }

        private static Formula Numeric(string code, decimal threshold, ExpressionOperator op) =>
            new BinaryFormula(
                new VariableFormula(new VariableRef(Guid.CreateVersion7(), code, ClinicalValueType.Numeric)),
                new NumericConstantFormula(threshold),
                op);

        private async Task<RiskFactor> SeedRiskFactorAsync(Guid pathogenId, Guid criterionId, bool isDeleted = false)
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

        private static readonly Formula AgeFormula = Numeric("AGE", 65, ExpressionOperator.GTE);
        private static readonly Formula HypotensionFormula = Numeric("SBP", 90, ExpressionOperator.LT);
        private static readonly Formula TachypneaFormula = Numeric("RR", 30, ExpressionOperator.GTE);

        private const string KlebsiellaName = "Klebsiella pneumoniae";
        private const string KlebsiellaDescription =
            "Gram-negative bacillus causing hospital-acquired pneumonia";
        private const string PneumococcusName = "Streptococcus pneumoniae";
        private const string PneumococcusDescription =
            "Gram-positive diplococcus, the most common community acquired pathogen";

        private async Task<(Guid PathogenId, Guid CriterionId, RiskFactor Factor)> SeedSingleAsync(
            bool isAtypicalPathogen = true, Formula? formula = null, bool softDeleteFactor = false)
        {
            var pathogenId = isAtypicalPathogen
                ? await SeedPathogenAsync(KlebsiellaName, KlebsiellaDescription, isAtypical: true)
                : await SeedPathogenAsync(PneumococcusName, PneumococcusDescription, isAtypical: false);
            var criterionId = await SeedCriterionAsync("Tuổi >= 65", formula ?? AgeFormula);
            var factor = await SeedRiskFactorAsync(pathogenId, criterionId, softDeleteFactor);
            return (pathogenId, criterionId, factor);
        }

        #region Happy path

        [Fact]
        public async Task GetRiskFactorById_ProjectsPathogenAndCriterion_Success()
        {
            var (pathogenId, criterionId, factor) = await SeedSingleAsync();

            var result = await _handler.HandleAsync(
                new GetRiskFactorByIdQuery { Id = factor.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(factor.Id, result.Data.Id);

            // Business rule: the read exposes the whole link, so the client can tell
            // which pathogen and which criterion the risk factor joins
            Assert.Equal(pathogenId, result.Data.Pathogen.Id);
            Assert.Equal(KlebsiellaName, result.Data.Pathogen.Name);
            Assert.True(result.Data.Pathogen.IsAtypical);

            Assert.Equal(criterionId, result.Data.Criterion.Id);
            Assert.Equal("Tuổi >= 65", result.Data.Criterion.Name);
            // The stored JSONB payload is rendered back into its human readable form
            Assert.Equal("AGE ≥ 65", result.Data.Criterion.Formula);
        }

        public static readonly TheoryData<bool> AtypicalFlags =
        [
            // Boundary values of the IsAtypical flag
            true,
            false,
        ];

        [Theory]
        [MemberData(nameof(AtypicalFlags))]
        public async Task GetRiskFactorById_IsAtypicalProjected_Success(bool isAtypical)
        {
            var (pathogenId, _, factor) = await SeedSingleAsync(isAtypicalPathogen: isAtypical);

            var result = await _handler.HandleAsync(
                new GetRiskFactorByIdQuery { Id = factor.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            Assert.Equal(pathogenId, result.Data.Pathogen.Id);
            Assert.Equal(isAtypical, result.Data.Pathogen.IsAtypical);
        }

        [Fact]
        public async Task GetRiskFactorById_NestedFormula_RenderedWithParentheses_Success()
        {
            // CURB-65 age together with the hypotension threshold, joined by AND
            var formula = new BinaryFormula(AgeFormula, HypotensionFormula, ExpressionOperator.AND);
            var (_, _, factor) = await SeedSingleAsync(formula: formula);

            var result = await _handler.HandleAsync(
                new GetRiskFactorByIdQuery { Id = factor.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            // Business rule from Formula.Parenthesize: only leaves (constants and
            // variables) stay bare, every other operand is wrapped in parentheses
            Assert.Equal("(AGE ≥ 65) AND (SBP < 90)", result.Data.Criterion.Formula);
        }

        [Fact]
        public async Task GetRiskFactorById_TachypneaFormula_Rendered_Success()
        {
            // The tachypnea threshold of the CURB-65 score
            var (_, _, factor) = await SeedSingleAsync(formula: TachypneaFormula);

            var result = await _handler.HandleAsync(
                new GetRiskFactorByIdQuery { Id = factor.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            Assert.Equal("RR ≥ 30", result.Data.Criterion.Formula);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task GetRiskFactorById_UnknownId_Fail()
        {
            var result = await _handler.HandleAsync(
                new GetRiskFactorByIdQuery { Id = Guid.CreateVersion7() },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetRiskFactorById_EmptyId_Fail()
        {
            // Boundary: Guid.Empty can never match a stored risk factor
            var result = await _handler.HandleAsync(
                new GetRiskFactorByIdQuery { Id = Guid.Empty },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetRiskFactorById_SoftDeletedRiskFactor_Fail()
        {
            // The !IsDeleted query filter hides soft deleted rows, so reading one is
            // reported exactly like an unknown id
            var (_, _, factor) = await SeedSingleAsync(softDeleteFactor: true);

            var result = await _handler.HandleAsync(
                new GetRiskFactorByIdQuery { Id = factor.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
            Assert.Null(result.Data);
        }

        #endregion
    }
}
