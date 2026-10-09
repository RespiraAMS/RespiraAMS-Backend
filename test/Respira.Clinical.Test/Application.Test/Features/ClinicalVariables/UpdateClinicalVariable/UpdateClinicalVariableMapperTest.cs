using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.ClinicalVariables.UpdateClinicalVariable;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Application.Test.Features.ClinicalVariables.UpdateClinicalVariable
{
    public class UpdateClinicalVariableMapperTest
    {
        private readonly IUpdateMapper<ClinicalVariable, UpdateClinicalVariableCommand> _mapper =
            new UpdateClinicalVariableMapper(new FormulaMapper());

        #region Boolean mapping

        [Fact]
        public void MapModel_BooleanVariable_Success()
        {
            var model = CreateBooleanVariable();
            var command = CreateCommand(ClinicalValueType.Boolean, ClinicalVariableCategory.PersonalInformation);
            command.CanonicalUnit = "yes/no";

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(command.Code, model.Code);
            Assert.Equal(command.Name, model.Name);
            Assert.Equal(command.Description, model.Description);
            Assert.Equal(command.CanonicalUnit, model.CanonicalUnit);
            Assert.Equal(command.IsRequired, model.IsRequired);
            Assert.Equal(command.Category, model.Category);
            Assert.Equal(ClinicalValueType.Boolean, model.ValueType);
            Assert.Null(model.Prerequisite);
        }

        [Fact]
        public void MapModel_KeepsIdentityAndState_Success()
        {
            // Update rewrites the payload only: the identity, the TPH discriminator
            // and the soft delete state must survive the mapping untouched
            var model = CreateBooleanVariable();
            var originalId = model.Id;
            var command = CreateCommand(ClinicalValueType.Boolean, ClinicalVariableCategory.Clinical);

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Equal(originalId, model.Id);
            Assert.Equal(ClinicalValueType.Boolean, model.ValueType);
            Assert.False(model.IsDeleted);
            Assert.Null(model.DeletedAt);
        }

        #endregion

        #region Numeric mapping

        [Fact]
        public void MapModel_NumericVariableWithRanges_Success()
        {
            var model = CreateNumericVariable();
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                acceptedRange: CreateRange(0, 300),
                normalRange: CreateRange(90, 140));

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(command.Code, model.Code);
            Assert.Equal(command.Name, model.Name);
            Assert.Equal(command.Description, model.Description);
            Assert.Equal(command.CanonicalUnit, model.CanonicalUnit);
            Assert.Equal(command.IsRequired, model.IsRequired);
            Assert.Equal(command.Category, model.Category);
            Assert.Equal(ClinicalValueType.Numeric, model.ValueType);

            var numeric = Assert.IsType<NumericClinicalVariable>(model);

            // Boundary: the accepted range starts at the inclusive lower limit 0
            Assert.Equal(0m, numeric.AcceptedRange.Min);
            Assert.False(numeric.AcceptedRange.IsMinExclusive);
            Assert.Equal(300m, numeric.AcceptedRange.Max);
            Assert.False(numeric.AcceptedRange.IsMaxExclusive);

            Assert.NotNull(numeric.NormalRange);
            Assert.Equal(90m, numeric.NormalRange.Min);
            Assert.Equal(140m, numeric.NormalRange.Max);
            Assert.Equal("mmHg", numeric.NormalRange.Unit);
        }

        [Fact]
        public void MapModel_NumericVariableWithoutNormalRange_Success()
        {
            // Dropping the normal range is a valid update: it is optional
            var model = CreateNumericVariable();
            model.NormalRange = new Range
            {
                Min = 90,
                IsMinExclusive = false,
                Max = 140,
                IsMaxExclusive = false,
                Unit = "mmHg",
            };
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                acceptedRange: CreateRange(0, 300),
                normalRange: null);

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var numeric = Assert.IsType<NumericClinicalVariable>(model);
            Assert.Null(numeric.NormalRange);
            Assert.NotNull(numeric.AcceptedRange);
        }

        [Fact]
        public void MapModel_NumericVariableWithUnboundedAcceptedRange_Success()
        {
            // Boundary: decimal.MaxValue is the convention for an unbounded upper limit
            var model = CreateNumericVariable();
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                acceptedRange: CreateRange(0, decimal.MaxValue),
                normalRange: CreateRange(0, 10));

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var numeric = Assert.IsType<NumericClinicalVariable>(model);
            Assert.Equal(0m, numeric.AcceptedRange.Min);
            Assert.Equal(decimal.MaxValue, numeric.AcceptedRange.Max);
            Assert.Equal(0m, numeric.NormalRange!.Min);
            Assert.Equal(10m, numeric.NormalRange.Max);
        }

        [Fact]
        public void MapModel_NumericVariableWithExclusiveAndDegenerateRange_Success()
        {
            // Boundary: min == max survives the mapping untouched,
            // exclusive bounds are carried over as-is
            var model = CreateNumericVariable();
            var command = CreateCommand(
                ClinicalValueType.Numeric,
                ClinicalVariableCategory.Paraclinical,
                acceptedRange: CreateRange(140, 140, isMinExclusive: true, isMaxExclusive: true),
                normalRange: CreateRange(140, 140));

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var numeric = Assert.IsType<NumericClinicalVariable>(model);
            Assert.Equal(140m, numeric.AcceptedRange.Min);
            Assert.True(numeric.AcceptedRange.IsMinExclusive);
            Assert.Equal(140m, numeric.AcceptedRange.Max);
            Assert.True(numeric.AcceptedRange.IsMaxExclusive);
            Assert.Equal(140m, numeric.NormalRange!.Min);
            Assert.False(numeric.NormalRange.IsMinExclusive);
        }

        #endregion

        #region Categorical mapping

        [Fact]
        public void MapModel_CategoricalVariable_Success()
        {
            var model = CreateCategoricalVariable(["DETECTED", "NOT_DETECTED"]);
            var command = CreateCommand(
                ClinicalValueType.Categorical,
                ClinicalVariableCategory.Paraclinical,
                acceptedValues: ["DETECTED", "NOT_DETECTED", "INCONCLUSIVE"]);

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(command.Code, model.Code);
            Assert.Equal(command.Name, model.Name);
            Assert.Equal(command.Description, model.Description);
            Assert.Equal(command.CanonicalUnit, model.CanonicalUnit);
            Assert.Equal(command.IsRequired, model.IsRequired);
            Assert.Equal(command.Category, model.Category);
            Assert.Equal(ClinicalValueType.Categorical, model.ValueType);

            var categorical = Assert.IsType<CategoricalClinicalVariable>(model);
            Assert.Equal(3, categorical.AcceptedValues.Count);
            Assert.Equal("INCONCLUSIVE", categorical.AcceptedValues[2]);
        }

        [Fact]
        public void MapModel_CategoricalAcceptedValues_AreSanitized_Success()
        {
            // Business rule: CategoricalClinicalVariable keeps its accepted values in
            // the sanitized form (trimmed, upper cased, words joined by an underscore)
            // because IsValidValue() sanitizes the incoming value before comparing it.
            // CreateClinicalVariableMapper guarantees this through the entity constructor;
            // the update mapper has to guarantee it as well, otherwise the row it hands
            // to the change tracker is persisted with the raw values while a variable
            // created from the very same command is persisted with the sanitized ones.
            var model = CreateCategoricalVariable(["DETECTED", "NOT_DETECTED"]);
            var command = CreateCommand(
                ClinicalValueType.Categorical,
                ClinicalVariableCategory.Paraclinical,
                acceptedValues: ["detected", "not detected", "inconclusive"]);

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());

            var categorical = Assert.IsType<CategoricalClinicalVariable>(model);
            Assert.Equal(
                new[] { "DETECTED", "NOT_DETECTED", "INCONCLUSIVE" },
                categorical.AcceptedValues);
            Assert.True(categorical.IsValidValue("inconclusive"));
            Assert.False(categorical.IsValidValue("rejected"));
        }

        #endregion

        #region Prerequisite mapping

        [Fact]
        public void MapModel_NullPrerequisite_ClearsIt_Success()
        {
            // Update may remove the prerequisite entirely
            var model = CreateBooleanVariable();
            model.Prerequisite = new BooleanConstantFormula(true);
            var command = CreateCommand(ClinicalValueType.Boolean, ClinicalVariableCategory.PersonalInformation);

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.Null(model.Prerequisite);
        }

        [Fact]
        public void MapModel_ConstantPrerequisite_Success()
        {
            var model = CreateBooleanVariable();
            var command = CreateCommand(
                ClinicalValueType.Boolean,
                ClinicalVariableCategory.PersonalInformation,
                prerequisite: new FormulaDto { Constant = "true" });

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var formula = Assert.IsType<BooleanConstantFormula>(model.Prerequisite);
            Assert.True(formula.Constant);
        }

        [Fact]
        public void MapModel_VariablePrerequisite_Success()
        {
            var female = CreateBooleanVariable();
            female.Code = "FEMALE";
            var model = CreateBooleanVariable();
            var command = CreateCommand(
                ClinicalValueType.Boolean,
                ClinicalVariableCategory.PersonalInformation,
                prerequisite: new FormulaDto { Variable = female.Id });

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable> { female });

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var formula = Assert.IsType<VariableFormula>(model.Prerequisite);
            Assert.Equal(female.Id, formula.Variable.Id);
            Assert.Equal("FEMALE", formula.Variable.Code);
            Assert.Equal(ClinicalValueType.Boolean, formula.Variable.ValueType);
        }

        [Fact]
        public void MapModel_NestedFormulaPrerequisite_Success()
        {
            var sbp = CreateNumericVariable();
            var model = CreateBooleanVariable();
            var command = CreateCommand(
                ClinicalValueType.Boolean,
                ClinicalVariableCategory.PersonalInformation,
                prerequisite: new FormulaDto
                {
                    Operator = ExpressionOperator.GT,
                    Left = new FormulaDto { Variable = sbp.Id },
                    Right = new FormulaDto { Constant = "140" },
                });

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable> { sbp });

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var formula = Assert.IsType<BinaryFormula>(model.Prerequisite);
            Assert.Equal(ExpressionOperator.GT, formula.Operator);
            var left = Assert.IsType<VariableFormula>(formula.Left);
            Assert.Equal(sbp.Id, left.Variable.Id);
            Assert.Equal(140m, Assert.IsType<NumericConstantFormula>(formula.Right).Constant);
        }

        [Fact]
        public void MapModel_UnknownPrerequisiteVariable_Failure()
        {
            var model = CreateBooleanVariable();
            var command = CreateCommand(
                ClinicalValueType.Boolean,
                ClinicalVariableCategory.PersonalInformation,
                prerequisite: new FormulaDto { Variable = Guid.CreateVersion7() });

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        }

        [Fact]
        public void MapModel_InvalidPrerequisiteFormula_Failure()
        {
            // Binary operands of different result types make the domain model throw,
            // which the formula mapper turns into a failure result
            var model = CreateBooleanVariable();
            var command = CreateCommand(
                ClinicalValueType.Boolean,
                ClinicalVariableCategory.PersonalInformation,
                prerequisite: new FormulaDto
                {
                    Operator = ExpressionOperator.ADD,
                    Left = new FormulaDto { Constant = "true" },
                    Right = new FormulaDto { Constant = "1" },
                });

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        }

        [Fact]
        public void MapModel_EmptyPrerequisite_Failure()
        {
            var model = CreateBooleanVariable();
            var command = CreateCommand(
                ClinicalValueType.Boolean,
                ClinicalVariableCategory.PersonalInformation,
                prerequisite: new FormulaDto());

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        }

        #endregion

        #region Invalid dependencies and command

        [Fact]
        public void MapModel_WithoutDependencies_ThrowsNotImplementedException()
        {
            // The dependency-less overload cannot resolve the recursive formula tree,
            // it only exists to satisfy the mapper interface
            var model = CreateBooleanVariable();
            var command = CreateCommand(ClinicalValueType.Boolean, ClinicalVariableCategory.PersonalInformation);

            Assert.Throws<NotImplementedException>(() => _mapper.MapModel(model, command));
        }

        [Fact]
        public void MapModel_NullDependencies_ThrowsArgumentException()
        {
            var model = CreateBooleanVariable();
            var command = CreateCommand(ClinicalValueType.Boolean, ClinicalVariableCategory.PersonalInformation);

            Assert.Throws<ArgumentException>(() => _mapper.MapModel(model, command, null));
        }

        [Fact]
        public void MapModel_NonVariableListDependencies_ThrowsArgumentException()
        {
            var model = CreateBooleanVariable();
            var command = CreateCommand(ClinicalValueType.Boolean, ClinicalVariableCategory.PersonalInformation);

            Assert.Throws<ArgumentException>(
                () => _mapper.MapModel(model, command, new List<string> { "FEMALE" }));
        }

        [Fact]
        public void MapModel_NumericWithoutAcceptedRange_ThrowsArgumentException()
        {
            // The validator forbids it, so the mapper treats it as a programming error
            var model = CreateNumericVariable();
            var command = CreateCommand(ClinicalValueType.Numeric, ClinicalVariableCategory.Paraclinical);
            command.AcceptedRange = null;

            Assert.Throws<ArgumentException>(
                () => _mapper.MapModel(model, command, new List<ClinicalVariable>()));
        }

        [Fact]
        public void MapModel_InvalidModelValueType_ThrowsArgumentException()
        {
            // The switch is driven by the model (TPH discriminator), an undefined
            // value type is an unexpected state rather than a failure result
            var model = new UnknownValueTypeClinicalVariable
            {
                Code = "UNKNOWN-TYPE",
                Name = "Unknown type",
                Description = "Clinical variable with an undefined value type",
                IsRequired = false,
                Category = ClinicalVariableCategory.Clinical,
            };
            var command = CreateCommand(ClinicalValueType.Boolean, ClinicalVariableCategory.Clinical);

            Assert.Throws<ArgumentException>(
                () => _mapper.MapModel(model, command, new List<ClinicalVariable>()));
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
                    Code = "PREGNANT-OR-LACTATING",
                    Name = "Pregnant or lactating",
                    Description = "Whether the patient is currently pregnant or lactating",
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

        private static NumericClinicalVariable CreateNumericVariable()
        {
            return new NumericClinicalVariable
            {
                Id = Guid.CreateVersion7(),
                // LOINC 8480-6 - systolic blood pressure
                Code = "8480-6",
                Name = "Systolic blood pressure",
                Description = "Systolic blood pressure measured at the arm",
                CanonicalUnit = "mmHg",
                IsRequired = true,
                Category = ClinicalVariableCategory.Paraclinical,
                AcceptedRange = CreateRange(0, 300),
            };
        }

        private static BooleanClinicalVariable CreateBooleanVariable()
        {
            return new BooleanClinicalVariable
            {
                Id = Guid.CreateVersion7(),
                Code = "PREGNANT",
                Name = "Pregnant",
                Description = "Whether the patient is currently pregnant",
                IsRequired = false,
                Category = ClinicalVariableCategory.PersonalInformation,
            };
        }

        private static CategoricalClinicalVariable CreateCategoricalVariable(List<string> acceptedValues)
        {
            return new CategoricalClinicalVariable(acceptedValues)
            {
                Id = Guid.CreateVersion7(),
                // LOINC 94500-6 - SARS-CoV-2 RNA detected
                Code = "94500-6",
                Name = "SARS-CoV-2 RNA",
                Description = "SARS-CoV-2 RNA detected in respiratory specimen",
                IsRequired = true,
                Category = ClinicalVariableCategory.Paraclinical,
            };
        }

        private sealed class UnknownValueTypeClinicalVariable : ClinicalVariable
        {
            public override ClinicalValueType ValueType => (ClinicalValueType)999;

            public override bool IsValidValue(object? value) => false;
        }
    }
}
