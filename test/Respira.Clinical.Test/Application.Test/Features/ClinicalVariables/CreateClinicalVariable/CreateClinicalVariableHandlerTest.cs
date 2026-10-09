using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.ClinicalVariables.CreateClinicalVariable;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;
using FormulaDto = Respira.Clinical.Application.Features.Shared.ManageFormula.FormulaDto;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Application.Test.Features.ClinicalVariables.CreateClinicalVariable
{
    public class CreateClinicalVariableHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly CreateClinicalVariableHandler _handler;
        private readonly IDbContext _context;

        public CreateClinicalVariableHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var mapper = new CreateClinicalVariableMapper(new FormulaMapper());
            var logger = new Mock<ILogger<CreateClinicalVariableHandler>>().Object;

            // Initialize handler
            _handler = new(_context, mapper, logger);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Clear leftover data so the SingleAsync / CountAsync assertions are deterministic
            // across runs. IgnoreQueryFilters because soft deleted rows are hidden by the
            // query filter but still occupy the table
            await _context.ClinicalVariables.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        #region Happy path

        [Fact]
        public async Task CreateClinicalVariable_BooleanVariable_Success()
        {
            var command = new CreateClinicalVariableCommand
            {
                Code = "PREGNANT-OR-LACTATING",
                Name = "Pregnant or lactating",
                Description = "Whether the patient is currently pregnant or lactating",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsRequired = false,
                AcceptedValues = [],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.NotEqual(Guid.Empty, result.Data.Id);

            // Verify through a fresh context so the change tracker of the saving context
            // cannot mask whether the row was truly committed
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);

            var boolean = Assert.IsType<BooleanClinicalVariable>(saved);
            Assert.Equal("PREGNANT-OR-LACTATING", boolean.Code);
            Assert.Equal("Pregnant or lactating", boolean.Name);
            Assert.Equal("Whether the patient is currently pregnant or lactating", boolean.Description);
            Assert.Equal(ClinicalValueType.Boolean, boolean.ValueType);
            Assert.Equal(ClinicalVariableCategory.PersonalInformation, boolean.Category);
            Assert.False(boolean.IsRequired);
            Assert.Null(boolean.CanonicalUnit);
            Assert.False(boolean.IsDeleted);
            Assert.Null(boolean.Prerequisite);
        }

        [Fact]
        public async Task CreateClinicalVariable_NumericVariableWithNormalRange_Success()
        {
            var command = new CreateClinicalVariableCommand
            {
                Code = "8480-6",
                Name = "Systolic blood pressure",
                Description = "Systolic blood pressure measured at the arm",
                ValueType = ClinicalValueType.Numeric,
                Category = ClinicalVariableCategory.Clinical,
                IsRequired = true,
                CanonicalUnit = "mmHg",
                // The supplied range units are deliberately wrong: both ranges are
                // forced to the canonical unit of the variable
                AcceptedRange = new Range
                {
                    Min = 0,
                    IsMinExclusive = false,
                    Max = 300,
                    IsMaxExclusive = false,
                    Unit = "kPa",
                },
                NormalRange = new Range
                {
                    Min = 90,
                    IsMinExclusive = false,
                    Max = 140,
                    IsMaxExclusive = false,
                    Unit = "cmHg",
                },
                AcceptedValues = [],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.NotEqual(Guid.Empty, result.Data.Id);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);

            var numeric = Assert.IsType<NumericClinicalVariable>(saved);
            Assert.Equal("8480-6", numeric.Code);
            Assert.Equal("Systolic blood pressure", numeric.Name);
            Assert.Equal(ClinicalValueType.Numeric, numeric.ValueType);
            Assert.Equal(ClinicalVariableCategory.Clinical, numeric.Category);
            Assert.True(numeric.IsRequired);
            Assert.Equal("mmHg", numeric.CanonicalUnit);

            // Business rule: the range units always match the canonical unit
            Assert.Equal(0m, numeric.AcceptedRange.Min);
            Assert.False(numeric.AcceptedRange.IsMinExclusive);
            Assert.Equal(300m, numeric.AcceptedRange.Max);
            Assert.False(numeric.AcceptedRange.IsMaxExclusive);
            Assert.Equal("mmHg", numeric.AcceptedRange.Unit);

            Assert.Equal(90m, numeric.NormalRange!.Min);
            Assert.False(numeric.NormalRange.IsMinExclusive);
            Assert.Equal(140m, numeric.NormalRange.Max);
            Assert.False(numeric.NormalRange.IsMaxExclusive);
            Assert.Equal("mmHg", numeric.NormalRange.Unit);
        }

        [Fact]
        public async Task CreateClinicalVariable_NumericVariableWithoutNormalRange_Success()
        {
            var command = new CreateClinicalVariableCommand
            {
                Code = "8310-5",
                Name = "Body temperature",
                Description = "Body temperature measured axillary in degree Celsius",
                ValueType = ClinicalValueType.Numeric,
                Category = ClinicalVariableCategory.Clinical,
                IsRequired = true,
                CanonicalUnit = "°C",
                AcceptedRange = new Range
                {
                    Min = 35m,
                    IsMinExclusive = false,
                    Max = 42m,
                    IsMaxExclusive = false,
                    Unit = null,
                },
                NormalRange = null,
                AcceptedValues = [],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.NotEqual(Guid.Empty, result.Data.Id);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);

            var numeric = Assert.IsType<NumericClinicalVariable>(saved);
            Assert.Equal("8310-5", numeric.Code);
            Assert.Equal(ClinicalValueType.Numeric, numeric.ValueType);

            // The normal range is optional for a numeric variable
            Assert.Null(numeric.NormalRange);
            Assert.Equal(35m, numeric.AcceptedRange.Min);
            Assert.Equal(42m, numeric.AcceptedRange.Max);
            Assert.Equal("°C", numeric.AcceptedRange.Unit);
        }

        [Fact]
        public async Task CreateClinicalVariable_CategoricalVariable_Success()
        {
            var command = new CreateClinicalVariableCommand
            {
                Code = "94500-6",
                Name = "SARS-CoV-2 RNA",
                Description = "SARS-CoV-2 RNA detected in respiratory specimen",
                ValueType = ClinicalValueType.Categorical,
                Category = ClinicalVariableCategory.Paraclinical,
                IsRequired = true,
                AcceptedValues = ["detected", "not detected"],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.NotEqual(Guid.Empty, result.Data.Id);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);

            var categorical = Assert.IsType<CategoricalClinicalVariable>(saved);
            Assert.Equal("94500-6", categorical.Code);
            Assert.Equal(ClinicalValueType.Categorical, categorical.ValueType);
            Assert.Equal(ClinicalVariableCategory.Paraclinical, categorical.Category);
            Assert.True(categorical.IsRequired);

            // Business rule: accepted values are stored sanitized (upper case, words
            // joined by an underscore) so the value check stays case/space insensitive
            Assert.Equal(2, categorical.AcceptedValues.Count);
            Assert.Equal("DETECTED", categorical.AcceptedValues[0]);
            Assert.Equal("NOT_DETECTED", categorical.AcceptedValues[1]);
            Assert.True(categorical.IsValidValue("not detected"));
            Assert.False(categorical.IsValidValue("inconclusive"));
        }

        [Fact]
        public async Task CreateClinicalVariable_WithPrerequisiteVariable_Success()
        {
            var female = await SeedVariableAsync(new BooleanClinicalVariable
            {
                Code = "FEMALE",
                Name = "Female sex",
                Description = "Whether the patient is female",
                IsRequired = true,
                Category = ClinicalVariableCategory.PersonalInformation,
            });

            var command = new CreateClinicalVariableCommand
            {
                Code = "PREGNANT-OR-LACTATING",
                Name = "Pregnant or lactating",
                Description = "Whether the patient is currently pregnant or lactating",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsRequired = false,
                Prerequisite = new FormulaDto { Variable = female.Id },
                AcceptedValues = [],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.NotEqual(Guid.Empty, result.Data.Id);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);

            var formula = Assert.IsType<VariableFormula>(saved.Prerequisite);
            Assert.Equal(female.Id, formula.Variable.Id);
            Assert.Equal("FEMALE", formula.Variable.Code);
            Assert.Equal(ClinicalValueType.Boolean, formula.Variable.ValueType);
        }

        [Fact]
        public async Task CreateClinicalVariable_WithPrerequisiteBinaryFormula_Success()
        {
            var sbp = await SeedVariableAsync(new NumericClinicalVariable
            {
                Code = "8480-6",
                Name = "Systolic blood pressure",
                Description = "Systolic blood pressure measured at the arm",
                CanonicalUnit = "mmHg",
                IsRequired = true,
                Category = ClinicalVariableCategory.Clinical,
                AcceptedRange = new Range
                {
                    Min = 0,
                    IsMinExclusive = false,
                    Max = 300,
                    IsMaxExclusive = false,
                    Unit = "mmHg",
                },
            });

            var command = new CreateClinicalVariableCommand
            {
                Code = "HYPERTENSION-STAGE-2",
                Name = "Hypertension stage 2",
                Description = "Whether the systolic blood pressure is at stage 2 hypertension level",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.Clinical,
                IsRequired = false,
                Prerequisite = new FormulaDto
                {
                    Operator = ExpressionOperator.GT,
                    Left = new FormulaDto { Variable = sbp.Id },
                    Right = new FormulaDto { Constant = "140" },
                },
                AcceptedValues = [],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.NotEqual(Guid.Empty, result.Data.Id);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);

            // The prerequisite survives the jsonb round trip
            var formula = Assert.IsType<BinaryFormula>(saved.Prerequisite);
            Assert.Equal(ExpressionOperator.GT, formula.Operator);
            var left = Assert.IsType<VariableFormula>(formula.Left);
            Assert.Equal(sbp.Id, left.Variable.Id);
            Assert.Equal("8480-6", left.Variable.Code);
            Assert.Equal(140m, Assert.IsType<NumericConstantFormula>(formula.Right).Constant);
        }

        // Business rule: the normal range must be contained in the accepted range
        public static readonly TheoryData<Range, Range> ContainedNormalRanges =
        [
            // Same bounds on both sides, all inclusive
            (CreateRange(90, 140), CreateRange(90, 140)),
            // Normal range strictly inside a wider accepted range
            (CreateRange(0, 300), CreateRange(90, 140)),
            // Exclusive normal bounds inside inclusive accepted bounds
            (CreateRange(90, 140), CreateRange(90, 140, isMinExclusive: true, isMaxExclusive: true)),
            // Exclusive bounds on both ranges at the very same limits
            (
                CreateRange(90, 140, isMinExclusive: true, isMaxExclusive: true),
                CreateRange(90, 140, isMinExclusive: true, isMaxExclusive: true)),
            // Accepted range without an upper limit: decimal.MaxValue is the convention
            (CreateRange(0, decimal.MaxValue), CreateRange(7, 20)),
        ];

        [Theory]
        [MemberData(nameof(ContainedNormalRanges))]
        public async Task CreateClinicalVariable_NormalRangeWithinAcceptedRange_Success(
            Range acceptedRange, Range normalRange)
        {
            var command = CreateNumericCommand(acceptedRange, normalRange);

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.NotEqual(Guid.Empty, result.Data.Id);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);

            var numeric = Assert.IsType<NumericClinicalVariable>(saved);
            Assert.Equal(acceptedRange.Min, numeric.AcceptedRange.Min);
            Assert.Equal(normalRange.Min, numeric.NormalRange!.Min);
            Assert.Equal("mmHg", numeric.AcceptedRange.Unit);
            Assert.Equal("mmHg", numeric.NormalRange.Unit);
        }

        #endregion

        #region Fail path

        // Boundary cases around the containment rule: any element of the normal range
        // that is not accepted by the accepted range makes the whole rule fail
        public static readonly TheoryData<Range, Range> NotContainedNormalRanges =
        [
            // Normal range starts below the accepted lower bound
            (CreateRange(90, 140), CreateRange(85, 140)),
            // Normal range ends above the accepted upper bound
            (CreateRange(90, 140), CreateRange(90, 145)),
            // Accepted lower bound is exclusive while the normal range includes it
            (CreateRange(90, 140, isMinExclusive: true), CreateRange(90, 140)),
            // Accepted upper bound is exclusive while the normal range includes it
            (CreateRange(90, 140, isMaxExclusive: true), CreateRange(90, 140)),
            // Normal range entirely above the accepted range
            (CreateRange(0, 300), CreateRange(301, 320)),
        ];

        [Theory]
        [MemberData(nameof(NotContainedNormalRanges))]
        public async Task CreateClinicalVariable_NormalRangeOutsideAcceptedRange_Fail(
            Range acceptedRange, Range normalRange)
        {
            var command = CreateNumericCommand(acceptedRange, normalRange);

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The variable must not be persisted when the business rule is violated
            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.ClinicalVariables.CountAsync(
                TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateClinicalVariable_UnknownPrerequisiteVariable_Fail()
        {
            var command = new CreateClinicalVariableCommand
            {
                Code = "PREGNANT-OR-LACTATING",
                Name = "Pregnant or lactating",
                Description = "Whether the patient is currently pregnant or lactating",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsRequired = false,
                Prerequisite = new FormulaDto { Variable = Guid.CreateVersion7() },
                AcceptedValues = [],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.ClinicalVariables.CountAsync(
                TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateClinicalVariable_SoftDeletedPrerequisiteVariable_Fail()
        {
            // A soft deleted variable is hidden by the query filter, so referencing it
            // must be rejected exactly like an unknown one
            var female = await SeedVariableAsync(new BooleanClinicalVariable
            {
                Code = "FEMALE",
                Name = "Female sex",
                Description = "Whether the patient is female",
                IsRequired = true,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsDeleted = true,
                DeletedAt = DateTimeOffset.UtcNow,
            });

            var command = new CreateClinicalVariableCommand
            {
                Code = "PREGNANT-OR-LACTATING",
                Name = "Pregnant or lactating",
                Description = "Whether the patient is currently pregnant or lactating",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsRequired = false,
                Prerequisite = new FormulaDto { Variable = female.Id },
                AcceptedValues = [],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .CountAsync(x => !x.IsDeleted, TestContext.Current.CancellationToken));
            Assert.Equal(1, await freshContext.ClinicalVariables.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateClinicalVariable_InvalidPrerequisiteFormula_Fail()
        {
            // Binary operands of different result types make the domain model throw,
            // which the formula mapper reports as a failure result
            var command = new CreateClinicalVariableCommand
            {
                Code = "PREGNANT-OR-LACTATING",
                Name = "Pregnant or lactating",
                Description = "Whether the patient is currently pregnant or lactating",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsRequired = false,
                Prerequisite = new FormulaDto
                {
                    Operator = ExpressionOperator.ADD,
                    Left = new FormulaDto { Constant = "true" },
                    Right = new FormulaDto { Constant = "1" },
                },
                AcceptedValues = [],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.ClinicalVariables.CountAsync(
                TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateClinicalVariable_EmptyPrerequisite_Fail()
        {
            var command = new CreateClinicalVariableCommand
            {
                Code = "PREGNANT-OR-LACTATING",
                Name = "Pregnant or lactating",
                Description = "Whether the patient is currently pregnant or lactating",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsRequired = false,
                Prerequisite = new FormulaDto(),
                AcceptedValues = [],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.ClinicalVariables.CountAsync(
                TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateClinicalVariable_NumericWithoutAcceptedRange_Throws()
        {
            var command = new CreateClinicalVariableCommand
            {
                Code = "8480-6",
                Name = "Systolic blood pressure",
                Description = "Systolic blood pressure measured at the arm",
                ValueType = ClinicalValueType.Numeric,
                Category = ClinicalVariableCategory.Clinical,
                IsRequired = true,
                CanonicalUnit = "mmHg",
                AcceptedRange = null,
                NormalRange = null,
                AcceptedValues = [],
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.HandleAsync(command, TestContext.Current.CancellationToken));

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.ClinicalVariables.CountAsync(
                TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CreateClinicalVariable_InvalidValueType_Throws()
        {
            // Boundary: 999 is outside every defined ClinicalValueType member
            var command = new CreateClinicalVariableCommand
            {
                Code = "8480-6",
                Name = "Systolic blood pressure",
                Description = "Systolic blood pressure measured at the arm",
                ValueType = (ClinicalValueType)999,
                Category = ClinicalVariableCategory.Clinical,
                IsRequired = true,
                AcceptedRange = null,
                NormalRange = null,
                AcceptedValues = [],
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.HandleAsync(command, TestContext.Current.CancellationToken));

            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.ClinicalVariables.CountAsync(
                TestContext.Current.CancellationToken));
        }

        #endregion

        private async Task<ClinicalVariable> SeedVariableAsync(ClinicalVariable variable)
        {
            await _context.ClinicalVariables.AddAsync(variable, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return variable;
        }

        private static CreateClinicalVariableCommand CreateNumericCommand(
            Range acceptedRange,
            Range normalRange)
        {
            return new CreateClinicalVariableCommand
            {
                Code = "8480-6",
                Name = "Systolic blood pressure",
                Description = "Systolic blood pressure measured at the arm",
                ValueType = ClinicalValueType.Numeric,
                Category = ClinicalVariableCategory.Clinical,
                IsRequired = true,
                CanonicalUnit = "mmHg",
                AcceptedRange = acceptedRange,
                NormalRange = normalRange,
                AcceptedValues = [],
            };
        }

        private static Range CreateRange(
            decimal min,
            decimal max,
            bool isMinExclusive = false,
            bool isMaxExclusive = false,
            string? unit = "mmHg")
        {
            return new Range
            {
                Min = min,
                IsMinExclusive = isMinExclusive,
                Max = max,
                IsMaxExclusive = isMaxExclusive,
                Unit = unit,
            };
        }
    }
}
