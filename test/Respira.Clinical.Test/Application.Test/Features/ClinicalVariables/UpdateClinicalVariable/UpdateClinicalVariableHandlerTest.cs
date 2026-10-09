using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.ClinicalVariables.UpdateClinicalVariable;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;
using FormulaDto = Respira.Clinical.Application.Features.Shared.ManageFormula.FormulaDto;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Application.Test.Features.ClinicalVariables.UpdateClinicalVariable
{
    public class UpdateClinicalVariableHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly UpdateClinicalVariableHandler _handler;
        private readonly IDbContext _context;

        public UpdateClinicalVariableHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var mapper = new UpdateClinicalVariableMapper(new FormulaMapper());
            var logger = new Mock<ILogger<UpdateClinicalVariableHandler>>().Object;

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
        public async Task UpdateClinicalVariable_BooleanVariable_Success()
        {
            var female = await SeedVariableAsync(new BooleanClinicalVariable
            {
                Code = "FEMALE",
                Name = "Female sex",
                Description = "Whether the patient is female",
                IsRequired = true,
                Category = ClinicalVariableCategory.PersonalInformation,
            });
            var sbp = await SeedVariableAsync(CreateSystolicBloodPressure());

            var command = new UpdateClinicalVariableCommand
            {
                Id = female.Id,
                Code = "FEMALE",
                Name = "Female sex (reviewed)",
                Description = "Whether the patient was assigned female at birth",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.Clinical,
                IsRequired = false,
                CanonicalUnit = null,
                AcceptedRange = null,
                NormalRange = null,
                AcceptedValues = [],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == female.Id, TestContext.Current.CancellationToken);

            var boolean = Assert.IsType<BooleanClinicalVariable>(saved);
            Assert.Equal("FEMALE", boolean.Code);
            Assert.Equal("Female sex (reviewed)", boolean.Name);
            Assert.Equal("Whether the patient was assigned female at birth", boolean.Description);
            Assert.Equal(ClinicalVariableCategory.Clinical, boolean.Category);
            Assert.False(boolean.IsRequired);
            Assert.Null(boolean.CanonicalUnit);
            Assert.Null(boolean.Prerequisite);
            Assert.False(boolean.IsDeleted);
            Assert.Null(boolean.DeletedAt);

            // The unrelated variable of the same table must stay untouched
            var untouched = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == sbp.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Systolic blood pressure", untouched.Name);
            Assert.Equal(2, await freshContext.ClinicalVariables.CountAsync(
                TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task UpdateClinicalVariable_NumericRangeUnitsForcedToCanonicalUnit_Success()
        {
            // Business rule: the accepted range and the normal range unit are always
            // forced to the canonical unit of the variable, regardless of what is set
            var sbp = await SeedVariableAsync(CreateSystolicBloodPressure());

            var command = new UpdateClinicalVariableCommand
            {
                Id = sbp.Id,
                Code = "8480-6",
                Name = "Systolic blood pressure",
                Description = "Systolic blood pressure measured at the arm, seated",
                ValueType = ClinicalValueType.Numeric,
                Category = ClinicalVariableCategory.Clinical,
                IsRequired = true,
                CanonicalUnit = "mmHg",
                // Deliberately wrong units: both have to be rewritten to mmHg
                AcceptedRange = new Range
                {
                    // Boundary: both accepted bounds are inclusive
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
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == sbp.Id, TestContext.Current.CancellationToken);

            var numeric = Assert.IsType<NumericClinicalVariable>(saved);
            Assert.Equal("Systolic blood pressure measured at the arm, seated", numeric.Description);
            Assert.Equal("mmHg", numeric.CanonicalUnit);
            Assert.Equal("mmHg", numeric.AcceptedRange.Unit);
            Assert.Equal(0m, numeric.AcceptedRange.Min);
            Assert.False(numeric.AcceptedRange.IsMinExclusive);
            Assert.Equal(300m, numeric.AcceptedRange.Max);
            Assert.False(numeric.AcceptedRange.IsMaxExclusive);
            Assert.Equal("mmHg", numeric.NormalRange!.Unit);
            Assert.Equal(90m, numeric.NormalRange.Min);
            Assert.Equal(140m, numeric.NormalRange.Max);
        }

        [Fact]
        public async Task UpdateClinicalVariable_NumericWithoutNormalRange_Success()
        {
            // Dropping the normal range is a valid update: it stays optional
            var sbp = await SeedVariableAsync(CreateSystolicBloodPressure());

            var command = new UpdateClinicalVariableCommand
            {
                Id = sbp.Id,
                Code = "8480-6",
                Name = "Systolic blood pressure",
                Description = "Systolic blood pressure measured at the arm",
                ValueType = ClinicalValueType.Numeric,
                Category = ClinicalVariableCategory.Clinical,
                IsRequired = true,
                CanonicalUnit = "mmHg",
                AcceptedRange = CreateRange(0, 300),
                NormalRange = null,
                AcceptedValues = [],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == sbp.Id, TestContext.Current.CancellationToken);

            var numeric = Assert.IsType<NumericClinicalVariable>(saved);
            Assert.Null(numeric.NormalRange);
            Assert.NotNull(numeric.AcceptedRange);
            Assert.Equal(0m, numeric.AcceptedRange.Min);
            Assert.Equal(300m, numeric.AcceptedRange.Max);
        }

        [Fact]
        public async Task UpdateClinicalVariable_NumericUnboundedAcceptedRange_Success()
        {
            // Boundary: decimal.MaxValue is the convention for "no upper limit"
            var crp = await SeedVariableAsync(new NumericClinicalVariable
            {
                // LOINC 1988-5 - C reactive protein
                Code = "1988-5",
                Name = "C reactive protein",
                Description = "C reactive protein mass per volume in serum or plasma",
                CanonicalUnit = "mg/L",
                IsRequired = false,
                Category = ClinicalVariableCategory.Paraclinical,
                AcceptedRange = CreateRange(0, 100, unit: "mg/L"),
                NormalRange = CreateRange(0, 10, unit: "mg/L"),
            });

            var command = new UpdateClinicalVariableCommand
            {
                Id = crp.Id,
                Code = "1988-5",
                Name = "C reactive protein",
                Description = "C reactive protein mass per volume in serum or plasma",
                ValueType = ClinicalValueType.Numeric,
                Category = ClinicalVariableCategory.Paraclinical,
                IsRequired = true,
                CanonicalUnit = "mg/L",
                AcceptedRange = CreateRange(0, decimal.MaxValue, unit: "mg/L"),
                NormalRange = CreateRange(0, 10, unit: "mg/L"),
                AcceptedValues = [],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == crp.Id, TestContext.Current.CancellationToken);

            var numeric = Assert.IsType<NumericClinicalVariable>(saved);
            Assert.True(numeric.IsRequired);
            Assert.Equal(0m, numeric.AcceptedRange.Min);
            Assert.Equal(decimal.MaxValue, numeric.AcceptedRange.Max);
            Assert.Equal("mg/L", numeric.AcceptedRange.Unit);
            Assert.Equal(0m, numeric.NormalRange!.Min);
            Assert.Equal(10m, numeric.NormalRange.Max);
            Assert.Equal("mg/L", numeric.NormalRange.Unit);
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
            // Degenerate boundary: both ranges are a single accepted point
            (CreateRange(140, 140), CreateRange(140, 140)),
        ];

        [Theory]
        [MemberData(nameof(ContainedNormalRanges))]
        public async Task UpdateClinicalVariable_NormalRangeWithinAcceptedRange_Success(
            Range acceptedRange, Range normalRange)
        {
            var sbp = await SeedVariableAsync(CreateSystolicBloodPressure());

            var command = CreateNumericCommand(sbp.Id, acceptedRange, normalRange);

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == sbp.Id, TestContext.Current.CancellationToken);

            var numeric = Assert.IsType<NumericClinicalVariable>(saved);
            Assert.Equal(acceptedRange.Min, numeric.AcceptedRange.Min);
            Assert.Equal(normalRange.Min, numeric.NormalRange!.Min);
            Assert.Equal("mmHg", numeric.AcceptedRange.Unit);
            Assert.Equal("mmHg", numeric.NormalRange.Unit);
        }

        [Fact]
        public async Task UpdateClinicalVariable_CategoricalVariable_Success()
        {
            var sarsCov2 = await SeedVariableAsync(new CategoricalClinicalVariable(["DETECTED", "NOT_DETECTED"])
            {
                // LOINC 94500-6 - SARS-CoV-2 RNA detected
                Code = "94500-6",
                Name = "SARS-CoV-2 RNA",
                Description = "SARS-CoV-2 RNA detected in respiratory specimen",
                IsRequired = true,
                Category = ClinicalVariableCategory.Paraclinical,
            });

            var command = new UpdateClinicalVariableCommand
            {
                Id = sarsCov2.Id,
                Code = "94500-6",
                Name = "SARS-CoV-2 RNA (PCR)",
                Description = "SARS-CoV-2 RNA detected in nasopharyngeal swab by PCR",
                ValueType = ClinicalValueType.Categorical,
                Category = ClinicalVariableCategory.Paraclinical,
                IsRequired = false,
                CanonicalUnit = null,
                AcceptedRange = null,
                NormalRange = null,
                AcceptedValues = ["DETECTED", "NOT_DETECTED"],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == sarsCov2.Id, TestContext.Current.CancellationToken);

            var categorical = Assert.IsType<CategoricalClinicalVariable>(saved);
            Assert.Equal("94500-6", categorical.Code);
            Assert.Equal("SARS-CoV-2 RNA (PCR)", categorical.Name);
            Assert.Equal("SARS-CoV-2 RNA detected in nasopharyngeal swab by PCR", categorical.Description);
            Assert.Equal(ClinicalVariableCategory.Paraclinical, categorical.Category);
            Assert.False(categorical.IsRequired);
            Assert.Equal(2, categorical.AcceptedValues.Count);
            Assert.True(categorical.IsValidValue("not detected"));
            Assert.False(categorical.IsValidValue("inconclusive"));
        }

        [Fact]
        public async Task UpdateClinicalVariable_AddPrerequisite_Success()
        {
            var female = await SeedVariableAsync(new BooleanClinicalVariable
            {
                Code = "FEMALE",
                Name = "Female sex",
                Description = "Whether the patient is female",
                IsRequired = true,
                Category = ClinicalVariableCategory.PersonalInformation,
            });
            var pregnant = await SeedVariableAsync(new BooleanClinicalVariable
            {
                Code = "PREGNANT",
                Name = "Pregnant",
                Description = "Whether the patient is currently pregnant",
                IsRequired = false,
                Category = ClinicalVariableCategory.PersonalInformation,
            });

            var command = new UpdateClinicalVariableCommand
            {
                Id = pregnant.Id,
                Code = "PREGNANT",
                Name = "Pregnant",
                Description = "Whether the patient is currently pregnant",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsRequired = false,
                CanonicalUnit = null,
                AcceptedRange = null,
                NormalRange = null,
                AcceptedValues = [],
                Prerequisite = new FormulaDto { Variable = female.Id },
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == pregnant.Id, TestContext.Current.CancellationToken);

            var formula = Assert.IsType<VariableFormula>(saved.Prerequisite);
            Assert.Equal(female.Id, formula.Variable.Id);
            Assert.Equal("FEMALE", formula.Variable.Code);
            Assert.Equal(ClinicalValueType.Boolean, formula.Variable.ValueType);
        }

        [Fact]
        public async Task UpdateClinicalVariable_ChangePrerequisite_Success()
        {
            // HYPERTENSION-STAGE-2 goes from "female" to "<systolic blood pressure> > 140"
            var sbp = await SeedVariableAsync(CreateSystolicBloodPressure());
            var hypertension = await SeedVariableAsync(new BooleanClinicalVariable
            {
                Code = "HYPERTENSION-STAGE-2",
                Name = "Hypertension stage 2",
                Description = "Whether the systolic blood pressure is at stage 2 hypertension level",
                IsRequired = false,
                Category = ClinicalVariableCategory.Clinical,
                Prerequisite = new BooleanConstantFormula(true),
            });

            var command = new UpdateClinicalVariableCommand
            {
                Id = hypertension.Id,
                Code = "HYPERTENSION-STAGE-2",
                Name = "Hypertension stage 2",
                Description = "Whether the systolic blood pressure is at stage 2 hypertension level",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.Clinical,
                IsRequired = false,
                CanonicalUnit = null,
                AcceptedRange = null,
                NormalRange = null,
                AcceptedValues = [],
                Prerequisite = new FormulaDto
                {
                    Operator = ExpressionOperator.GT,
                    Left = new FormulaDto { Variable = sbp.Id },
                    Right = new FormulaDto { Constant = "140" },
                },
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == hypertension.Id, TestContext.Current.CancellationToken);

            // The prerequisite survives the jsonb round trip
            var formula = Assert.IsType<BinaryFormula>(saved.Prerequisite);
            Assert.Equal(ExpressionOperator.GT, formula.Operator);
            var left = Assert.IsType<VariableFormula>(formula.Left);
            Assert.Equal(sbp.Id, left.Variable.Id);
            Assert.Equal("8480-6", left.Variable.Code);
            Assert.Equal(140m, Assert.IsType<NumericConstantFormula>(formula.Right).Constant);
        }

        [Fact]
        public async Task UpdateClinicalVariable_RemovePrerequisite_Success()
        {
            var female = await SeedVariableAsync(new BooleanClinicalVariable
            {
                Code = "FEMALE",
                Name = "Female sex",
                Description = "Whether the patient is female",
                IsRequired = true,
                Category = ClinicalVariableCategory.PersonalInformation,
            });
            var pregnant = await SeedVariableAsync(new BooleanClinicalVariable
            {
                Code = "PREGNANT",
                Name = "Pregnant",
                Description = "Whether the patient is currently pregnant",
                IsRequired = false,
                Category = ClinicalVariableCategory.PersonalInformation,
                Prerequisite = new VariableFormula(female),
            });

            var command = new UpdateClinicalVariableCommand
            {
                Id = pregnant.Id,
                Code = "PREGNANT",
                Name = "Pregnant",
                Description = "Whether the patient is currently pregnant",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsRequired = false,
                CanonicalUnit = null,
                AcceptedRange = null,
                NormalRange = null,
                AcceptedValues = [],
                Prerequisite = null,
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == pregnant.Id, TestContext.Current.CancellationToken);
            Assert.Null(saved.Prerequisite);

            // The variable the prerequisite pointed at is not affected
            var untouched = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == female.Id, TestContext.Current.CancellationToken);
            Assert.False(untouched.IsDeleted);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task UpdateClinicalVariable_UnknownId_Fail()
        {
            var female = await SeedVariableAsync(new BooleanClinicalVariable
            {
                Code = "FEMALE",
                Name = "Female sex",
                Description = "Whether the patient is female",
                IsRequired = true,
                Category = ClinicalVariableCategory.PersonalInformation,
            });
            var unknownId = Guid.CreateVersion7();

            var command = new UpdateClinicalVariableCommand
            {
                Id = unknownId,
                Code = "FEMALE",
                Name = "Female sex",
                Description = "Whether the patient is female",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsRequired = true,
                AcceptedValues = [],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The real variable must stay untouched
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == female.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Female sex", saved.Name);
            Assert.Equal(1, await freshContext.ClinicalVariables.CountAsync(
                TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task UpdateClinicalVariable_EmptyId_Fail()
        {
            // Boundary: Guid.Empty can never match a stored variable
            var female = await SeedVariableAsync(new BooleanClinicalVariable
            {
                Code = "FEMALE",
                Name = "Female sex",
                Description = "Whether the patient is female",
                IsRequired = true,
                Category = ClinicalVariableCategory.PersonalInformation,
            });

            var command = new UpdateClinicalVariableCommand
            {
                Id = Guid.Empty,
                Code = "FEMALE",
                Name = "Female sex",
                Description = "Whether the patient is female",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsRequired = true,
                AcceptedValues = [],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == female.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Female sex", saved.Name);
        }

        [Fact]
        public async Task UpdateClinicalVariable_SoftDeletedVariable_Fail()
        {
            // The !IsDeleted query filter hides soft deleted rows, so updating one
            // is reported as a missing variable
            var female = await SeedVariableAsync(new BooleanClinicalVariable
            {
                Code = "FEMALE",
                Name = "Female sex",
                Description = "Whether the patient is female",
                IsRequired = true,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsDeleted = true,
                DeletedAt = DateTimeOffset.UtcNow.AddDays(-1),
            });

            var command = new UpdateClinicalVariableCommand
            {
                Id = female.Id,
                Code = "FEMALE",
                Name = "Female sex renamed",
                Description = "Whether the patient is female",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsRequired = false,
                AcceptedValues = [],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.IgnoreQueryFilters().SingleAsync(
                x => x.Id == female.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Female sex", saved.Name);
            Assert.True(saved.IsDeleted);
        }

        [Fact]
        public async Task UpdateClinicalVariable_ValueTypeMismatch_Fail()
        {
            // Business rule: the ClinicalVariable table uses TPH, so the value type
            // of an existing variable can never be changed
            var sbp = await SeedVariableAsync(CreateSystolicBloodPressure());

            var command = new UpdateClinicalVariableCommand
            {
                Id = sbp.Id,
                Code = "8480-6",
                Name = "Systolic blood pressure",
                Description = "Systolic blood pressure measured at the arm",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.Clinical,
                IsRequired = true,
                AcceptedValues = [],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);

            // The mismatch is detected before the model is touched
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == sbp.Id, TestContext.Current.CancellationToken);
            var numeric = Assert.IsType<NumericClinicalVariable>(saved);
            Assert.Equal("Systolic blood pressure measured at the arm", numeric.Description);
            Assert.Equal("mmHg", numeric.CanonicalUnit);
            Assert.Equal(0m, numeric.AcceptedRange.Min);
            Assert.Equal(300m, numeric.AcceptedRange.Max);
        }

        [Fact]
        public async Task UpdateClinicalVariable_InvalidValueType_Fail()
        {
            // Boundary: 999 is outside every defined ClinicalValueType member
            var female = await SeedVariableAsync(new BooleanClinicalVariable
            {
                Code = "FEMALE",
                Name = "Female sex",
                Description = "Whether the patient is female",
                IsRequired = true,
                Category = ClinicalVariableCategory.PersonalInformation,
            });

            var command = new UpdateClinicalVariableCommand
            {
                Id = female.Id,
                Code = "FEMALE",
                Name = "Female sex renamed",
                Description = "Whether the patient is female",
                ValueType = (ClinicalValueType)999,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsRequired = true,
                AcceptedValues = [],
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == female.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Female sex", saved.Name);
        }

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
        public async Task UpdateClinicalVariable_NormalRangeOutsideAcceptedRange_Fail(
            Range acceptedRange, Range normalRange)
        {
            var sbp = await SeedVariableAsync(CreateSystolicBloodPressure());
            var command = CreateNumericCommand(sbp.Id, acceptedRange, normalRange);
            command.Name = "Systolic blood pressure renamed";

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The containment check runs after the mapping but before the save, so the
            // database must still hold the original payload
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == sbp.Id, TestContext.Current.CancellationToken);
            var numeric = Assert.IsType<NumericClinicalVariable>(saved);
            Assert.Equal("Systolic blood pressure", numeric.Name);
            Assert.Equal("Systolic blood pressure measured at the arm", numeric.Description);
            Assert.Equal(0m, numeric.AcceptedRange.Min);
            Assert.Equal(300m, numeric.AcceptedRange.Max);
            Assert.Equal(90m, numeric.NormalRange!.Min);
            Assert.Equal(140m, numeric.NormalRange.Max);
        }

        [Fact]
        public async Task UpdateClinicalVariable_UnknownPrerequisiteVariable_Fail()
        {
            var female = await SeedVariableAsync(new BooleanClinicalVariable
            {
                Code = "FEMALE",
                Name = "Female sex",
                Description = "Whether the patient is female",
                IsRequired = true,
                Category = ClinicalVariableCategory.PersonalInformation,
            });

            var command = new UpdateClinicalVariableCommand
            {
                Id = female.Id,
                Code = "FEMALE",
                Name = "Female sex renamed",
                Description = "Whether the patient is female",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsRequired = true,
                AcceptedValues = [],
                Prerequisite = new FormulaDto { Variable = Guid.CreateVersion7() },
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == female.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Female sex", saved.Name);
            Assert.Null(saved.Prerequisite);
        }

        [Fact]
        public async Task UpdateClinicalVariable_SoftDeletedPrerequisiteVariable_Fail()
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
                DeletedAt = DateTimeOffset.UtcNow.AddDays(-1),
            });
            var pregnant = await SeedVariableAsync(new BooleanClinicalVariable
            {
                Code = "PREGNANT",
                Name = "Pregnant",
                Description = "Whether the patient is currently pregnant",
                IsRequired = false,
                Category = ClinicalVariableCategory.PersonalInformation,
            });

            var command = new UpdateClinicalVariableCommand
            {
                Id = pregnant.Id,
                Code = "PREGNANT",
                Name = "Pregnant",
                Description = "Whether the patient is currently pregnant",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsRequired = false,
                AcceptedValues = [],
                Prerequisite = new FormulaDto { Variable = female.Id },
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == pregnant.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Pregnant", saved.Name);
            Assert.Null(saved.Prerequisite);
        }

        [Fact]
        public async Task UpdateClinicalVariable_InvalidPrerequisiteFormula_Fail()
        {
            // Binary operands of different result types make the domain model throw,
            // which the formula mapper reports as a failure result
            var female = await SeedVariableAsync(new BooleanClinicalVariable
            {
                Code = "FEMALE",
                Name = "Female sex",
                Description = "Whether the patient is female",
                IsRequired = true,
                Category = ClinicalVariableCategory.PersonalInformation,
            });

            var command = new UpdateClinicalVariableCommand
            {
                Id = female.Id,
                Code = "FEMALE",
                Name = "Female sex renamed",
                Description = "Whether the patient is female",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsRequired = true,
                AcceptedValues = [],
                Prerequisite = new FormulaDto
                {
                    Operator = ExpressionOperator.ADD,
                    Left = new FormulaDto { Constant = "true" },
                    Right = new FormulaDto { Constant = "1" },
                },
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == female.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Female sex", saved.Name);
            Assert.Null(saved.Prerequisite);
        }

        [Fact]
        public async Task UpdateClinicalVariable_EmptyPrerequisite_Fail()
        {
            var female = await SeedVariableAsync(new BooleanClinicalVariable
            {
                Code = "FEMALE",
                Name = "Female sex",
                Description = "Whether the patient is female",
                IsRequired = true,
                Category = ClinicalVariableCategory.PersonalInformation,
            });

            var command = new UpdateClinicalVariableCommand
            {
                Id = female.Id,
                Code = "FEMALE",
                Name = "Female sex renamed",
                Description = "Whether the patient is female",
                ValueType = ClinicalValueType.Boolean,
                Category = ClinicalVariableCategory.PersonalInformation,
                IsRequired = true,
                AcceptedValues = [],
                Prerequisite = new FormulaDto(),
            };

            var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == female.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Female sex", saved.Name);
            Assert.Null(saved.Prerequisite);
        }

        [Fact]
        public async Task UpdateClinicalVariable_NumericWithoutAcceptedRange_Throws()
        {
            // The validator forbids it, so the mapper treats it as a programming error
            var sbp = await SeedVariableAsync(CreateSystolicBloodPressure());

            var command = new UpdateClinicalVariableCommand
            {
                Id = sbp.Id,
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
            var saved = await freshContext.ClinicalVariables.SingleAsync(
                x => x.Id == sbp.Id, TestContext.Current.CancellationToken);
            var numeric = Assert.IsType<NumericClinicalVariable>(saved);
            Assert.Equal("Systolic blood pressure measured at the arm", numeric.Description);
            Assert.Equal(0m, numeric.AcceptedRange.Min);
            Assert.Equal(300m, numeric.AcceptedRange.Max);
        }

        #endregion

        private async Task<ClinicalVariable> SeedVariableAsync(ClinicalVariable variable)
        {
            await _context.ClinicalVariables.AddAsync(variable, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return variable;
        }

        private static UpdateClinicalVariableCommand CreateNumericCommand(
            Guid id,
            Range acceptedRange,
            Range normalRange)
        {
            return new UpdateClinicalVariableCommand
            {
                Id = id,
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
                AcceptedRange = CreateRange(0, 300),
                NormalRange = CreateRange(90, 140),
            };
        }
    }
}
