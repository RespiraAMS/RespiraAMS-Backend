using Respira.Clinical.Application.Features.ClinicalVariables.UpdateClinicalVariable;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Enums;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Application.Test.Features.ClinicalVariables.UpdateClinicalVariable
{
    public class UpdateClinicalVariableValidatorTest
    {
        private readonly UpdateClinicalVariableValidator _validator = new();

        #region Valid command

        // Enum boundaries: first and last defined members of ClinicalValueType
        // combined with the boundaries of ClinicalVariableCategory
        public static readonly TheoryData<ClinicalValueType, ClinicalVariableCategory> ValidTypeCombos =
        [
            (ClinicalValueType.Numeric, ClinicalVariableCategory.Paraclinical),
            (ClinicalValueType.Boolean, ClinicalVariableCategory.PersonalInformation),
            (ClinicalValueType.Categorical, ClinicalVariableCategory.Clinical),
        ];

        [Theory]
        [MemberData(nameof(ValidTypeCombos))]
        public async Task UpdateClinicalVariable_ValidValueTypeAndCategory_Success(
            ClinicalValueType valueType, ClinicalVariableCategory category)
        {
            var result = await _validator.ValidateAsync(
                CreateCommand(valueType, category),
                TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpdateClinicalVariable_ValidId_Success()
        {
            // Boundary: a UUID v7 is the smallest shape of a valid non-empty id
            var command = CreateCommand(ClinicalValueType.Boolean, ClinicalVariableCategory.PersonalInformation);
            command.Id = Guid.CreateVersion7();

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpdateClinicalVariable_NumericWithNormalRangeInsideAcceptedRange_Success()
        {
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                normalRange: CreateRange(90, 140));

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpdateClinicalVariable_DegenerateRangeBoundaries_Success()
        {
            // Boundary: min == max is the last value accepted by the Min <= Max rule
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                acceptedRange: CreateRange(140, 140),
                normalRange: CreateRange(140, 140));

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpdateClinicalVariable_UnboundedUpperAcceptedRange_Success()
        {
            // Boundary: decimal.MaxValue is the convention for "no upper limit"
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                acceptedRange: CreateRange(0, decimal.MaxValue));

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpdateClinicalVariable_ExclusiveRangeBounds_Success()
        {
            // Exclusive bounds are valid as long as min <= max
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                acceptedRange: CreateRange(0, 300, isMinExclusive: true, isMaxExclusive: true),
                normalRange: CreateRange(90, 140, isMinExclusive: true, isMaxExclusive: true));

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpdateClinicalVariable_NullRangeUnit_Success()
        {
            // A range unit is optional: only an empty (or whitespace) unit is rejected
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                acceptedRange: CreateRange(0, 300, unit: null));

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpdateClinicalVariable_NumericWithoutNormalRange_Success()
        {
            // The normal range is optional for a numeric clinical variable
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                normalRange: null);

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpdateClinicalVariable_VariablePrerequisite_Success()
        {
            var command = CreateCommand(
                ClinicalValueType.Boolean,
                ClinicalVariableCategory.PersonalInformation,
                prerequisite: new FormulaDto { Variable = Guid.CreateVersion7() });

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpdateClinicalVariable_BinaryPrerequisite_Success()
        {
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                prerequisite: new FormulaDto
                {
                    Operator = ExpressionOperator.GT,
                    Left = new FormulaDto { Variable = Guid.CreateVersion7() },
                    Right = new FormulaDto { Constant = "140" },
                });

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpdateClinicalVariable_CategoricalWithAcceptedValues_Success()
        {
            var command = CreateCommand(
                ClinicalValueType.Categorical,
                ClinicalVariableCategory.Clinical,
                acceptedValues: ["detected", "not detected"]);

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid required fields

        [Fact]
        public async Task UpdateClinicalVariable_EmptyId_Fail()
        {
            // Boundary: Guid.Empty is the only value rejected by NotEmpty on a Guid
            var command = CreateCommand(ClinicalValueType.Boolean, ClinicalVariableCategory.PersonalInformation);
            command.Id = Guid.Empty;

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Id", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData("")]
        // Whitespace-only value is treated as empty by NotEmpty
        [InlineData("   ")]
        public async Task UpdateClinicalVariable_EmptyCode_Fail(string code)
        {
            var command = CreateCommand(ClinicalValueType.Boolean, ClinicalVariableCategory.PersonalInformation);
            command.Code = code;

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Code", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateClinicalVariable_EmptyName_Fail(string name)
        {
            var command = CreateCommand(ClinicalValueType.Boolean, ClinicalVariableCategory.PersonalInformation);
            command.Name = name;

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Name", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateClinicalVariable_EmptyDescription_Fail(string description)
        {
            var command = CreateCommand(ClinicalValueType.Boolean, ClinicalVariableCategory.PersonalInformation);
            command.Description = description;

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Description", result.Errors[0].PropertyName);
        }

        #endregion

        #region Invalid enum values

        [Fact]
        public async Task UpdateClinicalVariable_ValueTypeNotInEnum_Fail()
        {
            // Boundary: 999 is outside every defined ClinicalValueType member
            var command = CreateCommand(
                (ClinicalValueType)999,
                ClinicalVariableCategory.Clinical,
                acceptedValues: []);

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("ValueType", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateClinicalVariable_CategoryNotInEnum_Fail()
        {
            // Boundary: 999 is outside every defined ClinicalVariableCategory member
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                (ClinicalVariableCategory)999);

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Category", result.Errors[0].PropertyName);
        }

        #endregion

        #region Invalid ranges

        [Fact]
        public async Task UpdateClinicalVariable_NumericWithoutAcceptedRange_Fail()
        {
            // The accepted range is mandatory for a numeric clinical variable
            var command = CreateCommand(ClinicalValueType.Numeric, ClinicalVariableCategory.Paraclinical);
            command.AcceptedRange = null;

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("AcceptedRange", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateClinicalVariable_AcceptedRangeMinGreaterThanMax_Fail()
        {
            // Boundary: one unit above the valid min == max boundary
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                acceptedRange: CreateRange(300, 90));

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("AcceptedRange", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task UpdateClinicalVariable_AcceptedRangeEmptyUnit_Fail(string unit)
        {
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                acceptedRange: CreateRange(0, 300, unit: unit));

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("AcceptedRange.Unit", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData(ClinicalValueType.Boolean)]
        [InlineData(ClinicalValueType.Categorical)]
        public async Task UpdateClinicalVariable_NonNumericWithNormalRange_Fail(ClinicalValueType valueType)
        {
            // A normal range only makes sense for a numeric clinical variable
            var command = CreateCommand(
                valueType,
                ClinicalVariableCategory.PersonalInformation,
                normalRange: CreateRange(90, 140));

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            // Whole-object rule: RuleFor(x => x) carries no property name
            Assert.Equal(string.Empty, result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateClinicalVariable_NormalRangeMinGreaterThanMax_Fail()
        {
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                normalRange: CreateRange(140, 90));

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("NormalRange", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task UpdateClinicalVariable_NormalRangeEmptyUnit_Fail(string unit)
        {
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                normalRange: CreateRange(90, 140, unit: unit));

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("NormalRange.Unit", result.Errors[0].PropertyName);
        }

        #endregion

        #region Invalid accepted values

        [Fact]
        public async Task UpdateClinicalVariable_CategoricalWithoutAcceptedValues_Fail()
        {
            // Boundary: a categorical variable needs at least one accepted value
            var command = CreateCommand(
                ClinicalValueType.Categorical,
                ClinicalVariableCategory.Clinical,
                acceptedValues: []);

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("AcceptedValues", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateClinicalVariable_CategoricalWithEmptyAcceptedValue_Fail()
        {
            var command = CreateCommand(
                ClinicalValueType.Categorical,
                ClinicalVariableCategory.Clinical,
                acceptedValues: ["DETECTED", ""]);

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("AcceptedValues[1]", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateClinicalVariable_BooleanWithoutAcceptedValues_Success()
        {
            // The AcceptedValues rule only fires for a categorical variable
            var command = CreateCommand(
                ClinicalValueType.Boolean,
                ClinicalVariableCategory.PersonalInformation,
                acceptedValues: []);

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid prerequisite

        [Fact]
        public async Task UpdateClinicalVariable_EmptyPrerequisite_Fail()
        {
            var command = CreateCommand(
                ClinicalValueType.Boolean,
                ClinicalVariableCategory.PersonalInformation,
                prerequisite: new FormulaDto());

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Prerequisite", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateClinicalVariable_MixedShapePrerequisite_Fail()
        {
            // A formula must be exactly one of constant / variable / binary / unary / ternary
            var command = CreateCommand(
                ClinicalValueType.Boolean,
                ClinicalVariableCategory.PersonalInformation,
                prerequisite: new FormulaDto
                {
                    Constant = "true",
                    Variable = Guid.CreateVersion7(),
                });

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Equal(2, result.Errors.Count);
            Assert.All(result.Errors, x => Assert.Equal("Prerequisite", x.PropertyName));
        }

        [Fact]
        public async Task UpdateClinicalVariable_EmptyGuidPrerequisiteVariable_Fail()
        {
            var command = CreateCommand(
                ClinicalValueType.Boolean,
                ClinicalVariableCategory.PersonalInformation,
                prerequisite: new FormulaDto { Variable = Guid.Empty });

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Prerequisite.Variable", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateClinicalVariable_PrerequisiteOperatorNotInEnum_Fail()
        {
            // Boundary: 999 is outside every defined ExpressionOperator member
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                prerequisite: new FormulaDto
                {
                    Operator = (ExpressionOperator)999,
                    Left = new FormulaDto { Variable = Guid.CreateVersion7() },
                    Right = new FormulaDto { Constant = "140" },
                });

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Prerequisite.Operator", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateClinicalVariable_EmptyNestedPrerequisite_Fail()
        {
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                prerequisite: new FormulaDto
                {
                    Operator = ExpressionOperator.GT,
                    Left = new FormulaDto(),
                    Right = new FormulaDto { Constant = "140" },
                });

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Prerequisite.Left", result.Errors[0].PropertyName);
        }

        #endregion

        private static UpdateClinicalVariableCommand CreateCommand(
            ClinicalValueType valueType,
            ClinicalVariableCategory category,
            Range? acceptedRange = null,
            Range? normalRange = null,
            List<string>? acceptedValues = null,
            FormulaDto? prerequisite = null)
        {
            return valueType switch
            {
                ClinicalValueType.Numeric => new UpdateClinicalVariableCommand
                {
                    // LOINC 8480-6 - systolic blood pressure
                    Id = Guid.CreateVersion7(),
                    Code = "8480-6",
                    Name = "Systolic blood pressure",
                    Description = "Systolic blood pressure measured at the arm",
                    ValueType = ClinicalValueType.Numeric,
                    Category = category,
                    IsRequired = true,
                    CanonicalUnit = "mmHg",
                    AcceptedRange = acceptedRange ?? CreateRange(0, 300),
                    NormalRange = normalRange,
                    AcceptedValues = [],
                    Prerequisite = prerequisite,
                },
                ClinicalValueType.Boolean => new UpdateClinicalVariableCommand
                {
                    Id = Guid.CreateVersion7(),
                    Code = "PREGNANT",
                    Name = "Pregnant",
                    Description = "Whether the patient is currently pregnant",
                    ValueType = ClinicalValueType.Boolean,
                    Category = category,
                    IsRequired = false,
                    CanonicalUnit = null,
                    AcceptedRange = null,
                    NormalRange = normalRange,
                    AcceptedValues = [],
                    Prerequisite = prerequisite,
                },
                _ => new UpdateClinicalVariableCommand
                {
                    // LOINC 94500-6 - SARS-CoV-2 RNA detected
                    Id = Guid.CreateVersion7(),
                    Code = "94500-6",
                    Name = "SARS-CoV-2 RNA",
                    Description = "SARS-CoV-2 RNA detected in respiratory specimen",
                    ValueType = valueType,
                    Category = category,
                    IsRequired = true,
                    CanonicalUnit = null,
                    AcceptedRange = null,
                    NormalRange = normalRange,
                    AcceptedValues = acceptedValues ?? ["detected", "not detected"],
                    Prerequisite = prerequisite,
                },
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
