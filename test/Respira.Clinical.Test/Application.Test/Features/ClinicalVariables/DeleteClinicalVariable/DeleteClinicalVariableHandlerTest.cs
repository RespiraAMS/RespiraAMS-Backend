using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.ClinicalVariables.DeleteClinicalVariable;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Application.Test.Features.ClinicalVariables.DeleteClinicalVariable
{
    public class DeleteClinicalVariableHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly DeleteClinicalVariableHandler _handler;
        private readonly IDbContext _context;

        public DeleteClinicalVariableHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var logger = new Mock<ILogger<DeleteClinicalVariableHandler>>().Object;

            // Initialize handler
            _handler = new(_context, logger);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Clear leftover data so the SingleAsync / CountAsync assertions are deterministic
            // across runs. IgnoreQueryFilters because soft-deleted rows are hidden by the
            // query filter but still occupy the table
            await _context.ClinicalVariables.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        #region Happy path

        [Fact]
        public async Task DeleteClinicalVariable_WithoutPrerequisiteReferences_Success()
        {
            // Lower boundary of the cascade: no variable references the deleted one
            var sbp = CreateSystolicBloodPressure();
            var female = CreateBooleanVariable(
                "FEMALE",
                "Female sex",
                "Whether the patient is female",
                ClinicalVariableCategory.PersonalInformation);
            await SeedAsync(sbp, female);

            var result = await _handler.HandleAsync(
                new DeleteClinicalVariableCommand { Id = sbp.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            // All entities carry a !IsDeleted query filter, so IgnoreQueryFilters is
            // required to observe the soft-delete flags
            await using var freshContext = new ClinicalDbContext(_options);

            var deleted = await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == sbp.Id, TestContext.Current.CancellationToken);
            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);

            // The row is a soft delete: it stays in the table but disappears from the
            // default query used by every other feature
            Assert.False(await freshContext.ClinicalVariables
                .AnyAsync(x => x.Id == sbp.Id, TestContext.Current.CancellationToken));

            // The unrelated variable must stay untouched
            var untouched = await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == female.Id, TestContext.Current.CancellationToken);
            Assert.False(untouched.IsDeleted);
            Assert.Null(untouched.DeletedAt);
        }

        [Fact]
        public async Task DeleteClinicalVariable_SinglePrerequisiteReference_PrerequisiteFormulaToNull_Success()
        {
            var sbp = CreateSystolicBloodPressure();
            var female = CreateBooleanVariable(
                "FEMALE",
                "Female sex",
                "Whether the patient is female",
                ClinicalVariableCategory.PersonalInformation);
            var pregnantOrLactating = CreateBooleanVariable(
                "PREGNANT-OR-LACTATING",
                "Pregnant or lactating",
                "Whether the patient is currently pregnant or lactating",
                ClinicalVariableCategory.PersonalInformation,
                new VariableFormula(female));
            await SeedAsync(sbp, female, pregnantOrLactating);

            var result = await _handler.HandleAsync(
                new DeleteClinicalVariableCommand { Id = female.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);

            // The target and its single referencing variable are soft deleted
            var deletedTarget = await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == female.Id, TestContext.Current.CancellationToken);
            Assert.True(deletedTarget.IsDeleted);
            Assert.NotNull(deletedTarget.DeletedAt);

            var reference = await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == pregnantOrLactating.Id, TestContext.Current.CancellationToken);
            Assert.False(reference.IsDeleted);
            Assert.Null(reference.DeletedAt);
            Assert.Null(reference.Prerequisite);
        }

        [Fact]
        public async Task DeleteClinicalVariable_NestedPrerequisiteReference_PrerequisiteToNull_Success()
        {
            // The reference can sit at any depth of the prerequisite formula:
            // PREGNANCY-INDUCED-HYPERTENSION = FEMALE AND (8480-6 > 140)
            var sbp = CreateSystolicBloodPressure();
            var female = CreateBooleanVariable(
                "FEMALE",
                "Female sex",
                "Whether the patient is female",
                ClinicalVariableCategory.PersonalInformation);
            var pih = CreateBooleanVariable(
                "PREGNANCY-INDUCED-HYPERTENSION",
                "Pregnancy induced hypertension",
                "Whether the patient has pregnancy induced hypertension",
                ClinicalVariableCategory.Clinical,
                new BinaryFormula(
                    new VariableFormula(female),
                    new BinaryFormula(
                        new VariableFormula(sbp),
                        new NumericConstantFormula(140),
                        ExpressionOperator.GT),
                    ExpressionOperator.AND));
            await SeedAsync(sbp, female, pih);

            var result = await _handler.HandleAsync(
                new DeleteClinicalVariableCommand { Id = female.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);

            var deletedTarget = await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == female.Id, TestContext.Current.CancellationToken);
            Assert.True(deletedTarget.IsDeleted);
            Assert.NotNull(deletedTarget.DeletedAt);

            // The nested reference is collected from the whole formula tree
            var reference = await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == pih.Id, TestContext.Current.CancellationToken);
            Assert.False(reference.IsDeleted);
            Assert.Null(reference.DeletedAt);
            Assert.Null(reference.Prerequisite);

            // The other variable of the very same formula is only referenced, not deleted
            Assert.False(await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == sbp.Id && x.IsDeleted, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteClinicalVariable_MultiplePrerequisiteReferences_PrerequisiteToNull_Success()
        {
            // Upper boundary of the cascade: two variables reference the deleted one
            var sbp = CreateSystolicBloodPressure();
            var female = CreateBooleanVariable(
                "FEMALE",
                "Female sex",
                "Whether the patient is female",
                ClinicalVariableCategory.PersonalInformation);
            var hypertension = CreateBooleanVariable(
                "HYPERTENSION-STAGE-2",
                "Hypertension stage 2",
                "Whether the systolic blood pressure is at stage 2 hypertension level",
                ClinicalVariableCategory.Clinical,
                new BinaryFormula(
                    new VariableFormula(sbp),
                    new NumericConstantFormula(140),
                    ExpressionOperator.GT));
            var hypotension = CreateBooleanVariable(
                "HYPOTENSION",
                "Hypotension",
                "Whether the systolic blood pressure is below the hypotension threshold",
                ClinicalVariableCategory.Clinical,
                new BinaryFormula(
                    new VariableFormula(sbp),
                    new NumericConstantFormula(90),
                    ExpressionOperator.LT));
            await SeedAsync(sbp, female, hypertension, hypotension);

            var result = await _handler.HandleAsync(
                new DeleteClinicalVariableCommand { Id = sbp.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);

            // Target plus both referencing variables
            var deletedRows = await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .Where(x => x.IsDeleted)
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.Single(deletedRows);
            Assert.Contains(deletedRows, x => x.Id == sbp.Id);
            Assert.All(deletedRows, x => Assert.NotNull(x.DeletedAt));

            // The variable outside the prerequisite chain stays untouched
            Assert.False(await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == female.Id && x.IsDeleted, TestContext.Current.CancellationToken));

            // References must not be deleted and their prerequisite formulas must be null
            var dbHypertension = await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == hypertension.Id, TestContext.Current.CancellationToken);
            Assert.False(dbHypertension.IsDeleted);
            Assert.Null(dbHypertension.DeletedAt);
            Assert.Null(dbHypertension.Prerequisite);

            var dbHypotension = await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == hypotension.Id, TestContext.Current.CancellationToken);
            Assert.False(dbHypotension.IsDeleted);
            Assert.Null(dbHypotension.DeletedAt);
            Assert.Null(dbHypotension.Prerequisite);
        }

        [Fact]
        public async Task DeleteClinicalVariable_OnlyDirectPrerequisiteReferences_PrerequisiteToNull_Success()
        {
            // Cascade is direct only: SEVERE-HYPERTENSION references HYPERTENSION-STAGE-2,
            // which in turn references the deleted blood pressure variable
            var sbp = CreateSystolicBloodPressure();
            var hypertension = CreateBooleanVariable(
                "HYPERTENSION-STAGE-2",
                "Hypertension stage 2",
                "Whether the systolic blood pressure is at stage 2 hypertension level",
                ClinicalVariableCategory.Clinical,
                new BinaryFormula(
                    new VariableFormula(sbp),
                    new NumericConstantFormula(140),
                    ExpressionOperator.GT));
            var severeHypertension = CreateBooleanVariable(
                "SEVERE-HYPERTENSION",
                "Severe hypertension",
                "Whether the patient is already flagged as severe hypertension",
                ClinicalVariableCategory.Clinical,
                new VariableFormula(hypertension));
            await SeedAsync(sbp, hypertension, severeHypertension);

            var result = await _handler.HandleAsync(
                new DeleteClinicalVariableCommand { Id = sbp.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);

            var deletedRows = await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .Where(x => x.IsDeleted)
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.Single(deletedRows);
            Assert.Contains(deletedRows, x => x.Id == sbp.Id);

            var dbHypertension = await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == hypertension.Id, TestContext.Current.CancellationToken);
            Assert.False(dbHypertension.IsDeleted);
            Assert.Null(dbHypertension.DeletedAt);
            Assert.Null(dbHypertension.Prerequisite);

            // The transitive reference does not reference the deleted variable itself
            // its prerequisite formula should remain the same
            var untouched = await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == severeHypertension.Id, TestContext.Current.CancellationToken);
            Assert.False(untouched.IsDeleted);
            Assert.Null(untouched.DeletedAt);
            Assert.NotNull(untouched.Prerequisite);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task DeleteClinicalVariable_UnknownId_Fail()
        {
            var sbp = CreateSystolicBloodPressure();
            var pregnantOrLactating = CreateBooleanVariable(
                "PREGNANT-OR-LACTATING",
                "Pregnant or lactating",
                "Whether the patient is currently pregnant or lactating",
                ClinicalVariableCategory.PersonalInformation,
                new VariableFormula(sbp));
            await SeedAsync(sbp, pregnantOrLactating);
            var unknownId = Guid.CreateVersion7();

            var result = await _handler.HandleAsync(
                new DeleteClinicalVariableCommand { Id = unknownId }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Nothing must be soft-deleted when the target does not exist
            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken));
            Assert.Equal(2, await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteClinicalVariable_EmptyId_Fail()
        {
            // Boundary: Guid.Empty can never match a stored variable
            var sbp = CreateSystolicBloodPressure();
            await SeedAsync(sbp);

            var result = await _handler.HandleAsync(
                new DeleteClinicalVariableCommand { Id = Guid.Empty }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteClinicalVariable_AlreadyDeleted_Fail()
        {
            // The !IsDeleted query filter hides soft-deleted rows, so deleting twice
            // is reported as a missing variable
            var female = CreateBooleanVariable(
                "FEMALE",
                "Female sex",
                "Whether the patient is female",
                ClinicalVariableCategory.PersonalInformation);
            female.IsDeleted = true;
            female.DeletedAt = DateTimeOffset.UtcNow.AddDays(-1);
            var sbp = CreateSystolicBloodPressure();
            await SeedAsync(female, sbp);

            // PostgreSQL truncates the timestamp to microsecond precision, so compare
            // against the value that was actually persisted
            await using var seedContext = new ClinicalDbContext(_options);
            var storedDeletedAt = (await seedContext.ClinicalVariables.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == female.Id, TestContext.Current.CancellationToken)).DeletedAt;

            var result = await _handler.HandleAsync(
                new DeleteClinicalVariableCommand { Id = female.Id }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);

            // The already deleted row keeps its original timestamp and the live row
            // of the same table is not touched
            var untouched = await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == female.Id, TestContext.Current.CancellationToken);
            Assert.True(untouched.IsDeleted);
            Assert.Equal(storedDeletedAt, untouched.DeletedAt);
            Assert.False(await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == sbp.Id && x.IsDeleted, TestContext.Current.CancellationToken));
        }

        #endregion

        private async Task SeedAsync(params ClinicalVariable[] variables)
        {
            await _context.ClinicalVariables.AddRangeAsync(variables, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        private static BooleanClinicalVariable CreateBooleanVariable(
            string code,
            string name,
            string description,
            ClinicalVariableCategory category,
            Formula? prerequisite = null)
        {
            return new BooleanClinicalVariable
            {
                Code = code,
                Name = name,
                Description = description,
                IsRequired = false,
                Category = category,
                Prerequisite = prerequisite,
            };
        }

        private static NumericClinicalVariable CreateSystolicBloodPressure()
        {
            return new NumericClinicalVariable
            {
                // LOINC 8480-6 - systolic blood pressure
                Code = "8480-6",
                Name = "Systolic blood pressure",
                Description = "Systolic blood pressure measured at the arm",
                CanonicalUnit = "mmHg",
                IsRequired = true,
                Category = ClinicalVariableCategory.Clinical,
                // Boundary: both accepted bounds are inclusive
                AcceptedRange = new Range
                {
                    Min = 0,
                    IsMinExclusive = false,
                    Max = 300,
                    IsMaxExclusive = false,
                    Unit = "mmHg",
                },
                NormalRange = new Range
                {
                    Min = 90,
                    IsMinExclusive = false,
                    Max = 140,
                    IsMaxExclusive = false,
                    Unit = "mmHg",
                },
            };
        }
    }
}
