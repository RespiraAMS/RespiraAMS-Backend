using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.ClinicalVariables.CreateClinicalVariable;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Application.Test.Features.ClinicalVariables.CreateClinicalVariable
{
    public class CreateClinicalVariableMapperTest
    {
        private readonly ICreateMapper<CreateClinicalVariableCommand, ClinicalVariable> _mapper =
            new CreateClinicalVariableMapper(new FormulaMapper());

        #region Boolean mapping

        [Fact]
        public void ToModel_BooleanVariable_Success()
        {
            var command = CreateCommand(ClinicalValueType.Boolean, ClinicalVariableCategory.PersonalInformation);

            var result = _mapper.ToModel(command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var model = Assert.IsType<BooleanClinicalVariable>(result.Data);
            Assert.Equal(command.Code, model.Code);
            Assert.Equal(command.Name, model.Name);
            Assert.Equal(command.Description, model.Description);
            Assert.Equal(command.CanonicalUnit, model.CanonicalUnit);
            Assert.Equal(command.IsRequired, model.IsRequired);
            Assert.Equal(command.Category, model.Category);
            Assert.Equal(ClinicalValueType.Boolean, model.ValueType);
            Assert.Null(model.Prerequisite);
            Assert.NotEqual(Guid.Empty, model.Id);
            Assert.False(model.IsDeleted);
        }

        #endregion

        #region Numeric mapping

        [Fact]
        public void ToModel_NumericVariableWithRanges_Success()
        {
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                acceptedRange: CreateRange(0, 300),
                normalRange: CreateRange(90, 140));

            var result = _mapper.ToModel(command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var model = Assert.IsType<NumericClinicalVariable>(result.Data);
            Assert.Equal(command.Code, model.Code);
            Assert.Equal(command.Name, model.Name);
            Assert.Equal(command.Description, model.Description);
            Assert.Equal(command.CanonicalUnit, model.CanonicalUnit);
            Assert.Equal(command.IsRequired, model.IsRequired);
            Assert.Equal(command.Category, model.Category);
            Assert.Equal(ClinicalValueType.Numeric, model.ValueType);
            Assert.Null(model.Prerequisite);

            // Boundary: the accepted range starts at the inclusive lower limit 0
            Assert.Equal(0m, model.AcceptedRange.Min);
            Assert.False(model.AcceptedRange.IsMinExclusive);
            Assert.Equal(300m, model.AcceptedRange.Max);
            Assert.False(model.AcceptedRange.IsMaxExclusive);
            Assert.Equal("mmHg", model.AcceptedRange.Unit);

            Assert.NotNull(model.NormalRange);
            Assert.Equal(90m, model.NormalRange.Min);
            Assert.False(model.NormalRange.IsMinExclusive);
            Assert.Equal(140m, model.NormalRange.Max);
            Assert.False(model.NormalRange.IsMaxExclusive);
            Assert.Equal("mmHg", model.NormalRange.Unit);
        }

        [Fact]
        public void ToModel_NumericVariableWithoutNormalRange_Success()
        {
            // The normal range is optional for a numeric clinical variable
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                acceptedRange: CreateRange(0, 300));

            var result = _mapper.ToModel(command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var model = Assert.IsType<NumericClinicalVariable>(result.Data);
            Assert.NotNull(model.AcceptedRange);
            Assert.Null(model.NormalRange);
        }

        [Fact]
        public void ToModel_NumericVariableWithUnboundedAcceptedRange_Success()
        {
            // Boundary: decimal.MaxValue is the convention for an unbounded upper limit
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                acceptedRange: CreateRange(0, decimal.MaxValue));

            var result = _mapper.ToModel(command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var model = Assert.IsType<NumericClinicalVariable>(result.Data);
            Assert.Equal(0m, model.AcceptedRange.Min);
            Assert.Equal(decimal.MaxValue, model.AcceptedRange.Max);
        }

        [Fact]
        public void ToModel_NumericVariableWithExclusiveAndDegenerateRange_Success()
        {
            // Boundary: min == max survives the mapping untouched,
            // exclusive bounds are carried over as-is
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                acceptedRange: CreateRange(140, 140, isMinExclusive: true, isMaxExclusive: true),
                normalRange: CreateRange(140, 140));

            var result = _mapper.ToModel(command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var model = Assert.IsType<NumericClinicalVariable>(result.Data);
            Assert.Equal(140m, model.AcceptedRange.Min);
            Assert.True(model.AcceptedRange.IsMinExclusive);
            Assert.Equal(140m, model.AcceptedRange.Max);
            Assert.True(model.AcceptedRange.IsMaxExclusive);
            Assert.Equal(140m, model.NormalRange!.Min);
            Assert.False(model.NormalRange.IsMinExclusive);
        }

        #endregion

        #region Categorical mapping

        [Fact]
        public void ToModel_CategoricalVariable_Success()
        {
            // Accepted values are sanitized: trimmed, upper cased and space separated
            // words are joined with an underscore
            var command = CreateCommand(
                ClinicalValueType.Categorical,
                ClinicalVariableCategory.Paraclinical,
                acceptedValues: ["detected", "not detected"]);

            var result = _mapper.ToModel(command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var model = Assert.IsType<CategoricalClinicalVariable>(result.Data);
            Assert.Equal(command.Code, model.Code);
            Assert.Equal(command.Name, model.Name);
            Assert.Equal(command.Description, model.Description);
            Assert.Equal(command.CanonicalUnit, model.CanonicalUnit);
            Assert.Equal(command.IsRequired, model.IsRequired);
            Assert.Equal(command.Category, model.Category);
            Assert.Equal(ClinicalValueType.Categorical, model.ValueType);
            Assert.Equal(2, model.AcceptedValues.Count);
            Assert.Equal("DETECTED", model.AcceptedValues[0]);
            Assert.Equal("NOT_DETECTED", model.AcceptedValues[1]);
            Assert.Null(model.Prerequisite);
        }

        #endregion

        #region Prerequisite mapping

        [Fact]
        public void ToModel_ConstantPrerequisite_Success()
        {
            var command = CreateCommand(
                ClinicalValueType.Boolean,
                ClinicalVariableCategory.PersonalInformation,
                prerequisite: new FormulaDto { Constant = "true" });

            var result = _mapper.ToModel(command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var formula = Assert.IsType<BooleanConstantFormula>(result.Data.Prerequisite);
            Assert.True(formula.Constant);
        }

        [Fact]
        public void ToModel_VariablePrerequisite_Success()
        {
            var female = CreateBooleanVariable(Guid.CreateVersion7(), "FEMALE");
            var command = CreateCommand(
                ClinicalValueType.Boolean,
                ClinicalVariableCategory.PersonalInformation,
                prerequisite: new FormulaDto { Variable = female.Id });

            var result = _mapper.ToModel(command, new List<ClinicalVariable> { female });

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var formula = Assert.IsType<VariableFormula>(result.Data.Prerequisite);
            Assert.Equal(female.Id, formula.Variable.Id);
            Assert.Equal("FEMALE", formula.Variable.Code);
            Assert.Equal(ClinicalValueType.Boolean, formula.Variable.ValueType);
        }

        [Fact]
        public void ToModel_NestedFormulaPrerequisite_Success()
        {
            var sbp = CreateNumericVariable(Guid.CreateVersion7(), "8480-6");
            var command = CreateCommand(
                ClinicalValueType.Boolean,
                ClinicalVariableCategory.PersonalInformation,
                prerequisite: new FormulaDto
                {
                    Operator = ExpressionOperator.GT,
                    Left = new FormulaDto { Variable = sbp.Id },
                    Right = new FormulaDto { Constant = "140" },
                });

            var result = _mapper.ToModel(command, new List<ClinicalVariable> { sbp });

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var formula = Assert.IsType<BinaryFormula>(result.Data.Prerequisite);
            Assert.Equal(ExpressionOperator.GT, formula.Operator);
            var left = Assert.IsType<VariableFormula>(formula.Left);
            Assert.Equal(sbp.Id, left.Variable.Id);
            Assert.Equal(140m, Assert.IsType<NumericConstantFormula>(formula.Right).Constant);
        }

        [Fact]
        public void ToModel_UnknownPrerequisiteVariable_Failure()
        {
            var command = CreateCommand(
                ClinicalValueType.Boolean,
                ClinicalVariableCategory.PersonalInformation,
                prerequisite: new FormulaDto { Variable = Guid.CreateVersion7() });

            var result = _mapper.ToModel(command, new List<ClinicalVariable>());

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public void ToModel_InvalidPrerequisiteFormula_Failure()
        {
            // Binary operands of different result types make the domain model throw,
            // which the formula mapper turns into a failure result
            var command = CreateCommand(
                ClinicalValueType.Boolean,
                ClinicalVariableCategory.PersonalInformation,
                prerequisite: new FormulaDto
                {
                    Operator = ExpressionOperator.ADD,
                    Left = new FormulaDto { Constant = "true" },
                    Right = new FormulaDto { Constant = "1" },
                });

            var result = _mapper.ToModel(command, new List<ClinicalVariable>());

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public void ToModel_EmptyPrerequisite_Failure()
        {
            var command = CreateCommand(
                ClinicalValueType.Boolean,
                ClinicalVariableCategory.PersonalInformation,
                prerequisite: new FormulaDto());

            var result = _mapper.ToModel(command, new List<ClinicalVariable>());

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        #endregion

        #region Invalid dependencies and command

        [Fact]
        public void ToModel_WithoutDependencies_ThrowsNotImplementedException()
        {
            // The dependency-less overload cannot resolve the recursive formula tree,
            // it only exists to satisfy the mapper interface
            var command = CreateCommand(ClinicalValueType.Boolean, ClinicalVariableCategory.PersonalInformation);

            Assert.Throws<NotImplementedException>(() => _mapper.ToModel(command));
        }

        [Fact]
        public void ToModel_NullDependencies_ThrowsArgumentException()
        {
            var command = CreateCommand(ClinicalValueType.Boolean, ClinicalVariableCategory.PersonalInformation);

            Assert.Throws<ArgumentException>(() => _mapper.ToModel(command, null));
        }

        [Fact]
        public void ToModel_NonVariableListDependencies_ThrowsArgumentException()
        {
            var command = CreateCommand(ClinicalValueType.Boolean, ClinicalVariableCategory.PersonalInformation);

            Assert.Throws<ArgumentException>(() => _mapper.ToModel(command, new List<string> { "FEMALE" }));
        }

        [Fact]
        public void ToModel_NumericWithoutAcceptedRange_ThrowsArgumentException()
        {
            var command = CreateCommand(ClinicalValueType.Numeric, ClinicalVariableCategory.Paraclinical);
            command.AcceptedRange = null;

            Assert.Throws<ArgumentException>(
                () => _mapper.ToModel(command, new List<ClinicalVariable>()));
        }

        [Fact]
        public void ToModel_InvalidValueType_ThrowsArgumentException()
        {
            // Boundary: 999 is outside every defined ClinicalValueType member
            var command = CreateCommand(
                (ClinicalValueType)999,
                ClinicalVariableCategory.Clinical,
                acceptedValues: []);

            Assert.Throws<ArgumentException>(
                () => _mapper.ToModel(command, new List<ClinicalVariable>()));
        }

        #endregion

        private static CreateClinicalVariableCommand CreateCommand(
            ClinicalValueType valueType,
            ClinicalVariableCategory category,
            Range? acceptedRange = null,
            Range? normalRange = null,
            List<string>? acceptedValues = null,
            FormulaDto? prerequisite = null)
        {
            return valueType switch
            {
                ClinicalValueType.Numeric => new CreateClinicalVariableCommand
                {
                    // LOINC 8480-6 - systolic blood pressure
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
                ClinicalValueType.Boolean => new CreateClinicalVariableCommand
                {
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
                _ => new CreateClinicalVariableCommand
                {
                    // LOINC 94500-6 - SARS-CoV-2 RNA detected
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

        private static NumericClinicalVariable CreateNumericVariable(Guid id, string code)
        {
            return new NumericClinicalVariable
            {
                Id = id,
                Code = code,
                Name = code,
                Description = $"{code} clinical variable",
                IsRequired = false,
                Category = ClinicalVariableCategory.Paraclinical,
                AcceptedRange = new Range
                {
                    Min = 0,
                    IsMinExclusive = false,
                    Max = decimal.MaxValue,
                    IsMaxExclusive = false,
                    Unit = "mmHg",
                },
            };
        }

        private static BooleanClinicalVariable CreateBooleanVariable(Guid id, string code)
        {
            return new BooleanClinicalVariable
            {
                Id = id,
                Code = code,
                Name = code,
                Description = $"{code} clinical variable",
                IsRequired = false,
                Category = ClinicalVariableCategory.PersonalInformation,
            };
        }
    }
}
