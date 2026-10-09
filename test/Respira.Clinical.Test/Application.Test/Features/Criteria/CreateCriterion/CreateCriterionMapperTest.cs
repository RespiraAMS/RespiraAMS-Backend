using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Criteria.CreateCriterion;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;
using FormulaDto = Respira.Clinical.Application.Features.Shared.ManageFormula.FormulaDto;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Application.Test.Features.Criteria.CreateCriterion
{
    public class CreateCriterionMapperTest
    {
        private readonly ICreateMapper<CreateCriterionCommand, Criterion> _mapper =
            new CreateCriterionMapper(new FormulaMapper());

        #region Happy path

        [Fact]
        public void ToModel_BooleanConstant_Success()
        {
            var command = new CreateCriterionCommand
            {
                Name = "Universal triage screening",
                Formula = new FormulaDto { Constant = "false" },
            };

            var result = _mapper.ToModel(command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var criterion = Assert.IsType<Criterion>(result.Data);
            // Base generates the ID so the handler can return it right after saving
            Assert.NotEqual(Guid.Empty, criterion.Id);
            Assert.Equal("Universal triage screening", criterion.Name);
            Assert.False(criterion.IsDeleted);

            var formula = Assert.IsType<BooleanConstantFormula>(criterion.Formula);
            Assert.False(formula.Constant);
            Assert.Equal(ExpressionResultType.Boolean, formula.ResultType);
            Assert.Empty(criterion.Variables);
        }

        [Fact]
        public void ToModel_Variable_Success()
        {
            // LOINC 46098-0 - female sex
            var female = CreateBooleanVariable(Guid.CreateVersion7(), "46098-0");
            var command = new CreateCriterionCommand
            {
                Name = "Female-specific risk",
                Formula = new FormulaDto { Variable = female.Id },
            };

            var result = _mapper.ToModel(command, new List<ClinicalVariable> { female });

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var formula = Assert.IsType<VariableFormula>(result.Data.Formula);
            Assert.Equal(female.Id, formula.Variable.Id);
            Assert.Equal("46098-0", formula.Variable.Code);
            Assert.Equal(ClinicalValueType.Boolean, formula.Variable.ValueType);
            Assert.Equal(ExpressionResultType.Boolean, formula.ResultType);

            var variable = Assert.Single(result.Data.Variables);
            Assert.Equal("46098-0", variable.Code);
        }

        [Fact]
        public void ToModel_Comparison_Success()
        {
            // LOINC 8480-6 - systolic blood pressure above the hypertension threshold
            var sbp = CreateNumericVariable(Guid.CreateVersion7(), "8480-6", "mmHg");
            var command = new CreateCriterionCommand
            {
                Name = "Hypertension crisis",
                Formula = new FormulaDto
                {
                    Operator = ExpressionOperator.GT,
                    Left = new FormulaDto { Variable = sbp.Id },
                    Right = new FormulaDto { Constant = "140" },
                },
            };

            var result = _mapper.ToModel(command, new List<ClinicalVariable> { sbp });

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Equal("Hypertension crisis", result.Data.Name);

            // A comparison evaluates to a boolean, which is what a criterion must produce
            var formula = Assert.IsType<BinaryFormula>(result.Data.Formula);
            Assert.Equal(ExpressionOperator.GT, formula.Operator);
            Assert.Equal(ExpressionResultType.Boolean, formula.ResultType);
            Assert.Equal(140m, Assert.IsType<NumericConstantFormula>(formula.Right).Constant);
            Assert.Equal("8480-6 > 140", formula.ToString());

            var variable = Assert.Single(result.Data.Variables);
            Assert.Equal("8480-6", variable.Code);
        }

        [Fact]
        public void ToModel_NestedLogical_Success()
        {
            // Female sex AND systolic blood pressure above 140 mmHg
            var female = CreateBooleanVariable(Guid.CreateVersion7(), "46098-0");
            var sbp = CreateNumericVariable(Guid.CreateVersion7(), "8480-6", "mmHg");
            var command = new CreateCriterionCommand
            {
                Name = "Female hypertensive crisis",
                Formula = new FormulaDto
                {
                    Operator = ExpressionOperator.AND,
                    Left = new FormulaDto { Variable = female.Id },
                    Right = new FormulaDto
                    {
                        Operator = ExpressionOperator.GT,
                        Left = new FormulaDto { Variable = sbp.Id },
                        Right = new FormulaDto { Constant = "140" },
                    },
                },
            };

            var result = _mapper.ToModel(
                command, new List<ClinicalVariable> { female, sbp });

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var formula = Assert.IsType<BinaryFormula>(result.Data.Formula);
            Assert.Equal(ExpressionOperator.AND, formula.Operator);
            Assert.Equal(ExpressionResultType.Boolean, formula.ResultType);
            // Only the non-leaf operand is parenthesized
            Assert.Equal("46098-0 AND (8480-6 > 140)", formula.ToString());

            // Variables are de-duplicated by code
            Assert.Equal(2, result.Data.Variables.Count());
            Assert.Contains(result.Data.Variables, x => x.Code == "46098-0");
            Assert.Contains(result.Data.Variables, x => x.Code == "8480-6");
        }

        #endregion

        #region Fail path

        [Fact]
        public void ToModel_UnknownVariable_Failure()
        {
            var command = new CreateCriterionCommand
            {
                Name = "Female-specific risk",
                Formula = new FormulaDto { Variable = Guid.CreateVersion7() },
            };

            var result = _mapper.ToModel(command, new List<ClinicalVariable>());

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public void ToModel_EmptyFormula_Failure()
        {
            // Boundary: every field null cannot be resolved into any formula type
            var command = new CreateCriterionCommand
            {
                Name = "Hypertension crisis",
                Formula = new FormulaDto(),
            };

            var result = _mapper.ToModel(command, new List<ClinicalVariable>());

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public void ToModel_InvalidFormula_Failure()
        {
            // Boolean and numeric operands make the domain model throw, which the
            // formula mapper turns into a failure result
            var command = new CreateCriterionCommand
            {
                Name = "Hypertension crisis",
                Formula = new FormulaDto
                {
                    Operator = ExpressionOperator.ADD,
                    Left = new FormulaDto { Constant = "true" },
                    Right = new FormulaDto { Constant = "140" },
                },
            };

            var result = _mapper.ToModel(command, new List<ClinicalVariable>());

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        #endregion

        #region Invalid dependencies

        [Fact]
        public void ToModel_WithoutDependencies_ThrowsNotImplementedException()
        {
            // The dependency-less overload cannot resolve the recursive formula tree,
            // it only exists to satisfy the mapper interface
            var command = new CreateCriterionCommand
            {
                Name = "Universal triage screening",
                Formula = new FormulaDto { Constant = "true" },
            };

            Assert.Throws<NotImplementedException>(() => _mapper.ToModel(command));
        }

        [Fact]
        public void ToModel_NullDependencies_Failure()
        {
            // The dependency list is how variable ids get resolved, so without it no
            // formula can be built
            var command = new CreateCriterionCommand
            {
                Name = "Universal triage screening",
                Formula = new FormulaDto { Constant = "true" },
            };

            var result = _mapper.ToModel(command, null);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public void ToModel_NonVariableListDependencies_Failure()
        {
            var command = new CreateCriterionCommand
            {
                Name = "Universal triage screening",
                Formula = new FormulaDto { Constant = "true" },
            };

            var result = _mapper.ToModel(command, new List<string> { "46098-0" });

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        #endregion

        private static BooleanClinicalVariable CreateBooleanVariable(Guid id, string code)
        {
            return new BooleanClinicalVariable
            {
                Id = id,
                Code = code,
                Name = "Female sex",
                Description = "Whether the patient is female",
                IsRequired = true,
                Category = ClinicalVariableCategory.PersonalInformation,
            };
        }

        private static NumericClinicalVariable CreateNumericVariable(Guid id, string code, string unit)
        {
            return new NumericClinicalVariable
            {
                Id = id,
                Code = code,
                Name = "Systolic blood pressure",
                Description = "Systolic blood pressure measured at the arm",
                CanonicalUnit = unit,
                IsRequired = true,
                Category = ClinicalVariableCategory.Clinical,
                AcceptedRange = new Range
                {
                    Min = 0,
                    IsMinExclusive = false,
                    Max = 300,
                    IsMaxExclusive = false,
                    Unit = unit,
                },
                NormalRange = new Range
                {
                    Min = 90,
                    IsMinExclusive = false,
                    Max = 140,
                    IsMaxExclusive = false,
                    Unit = unit,
                },
            };
        }
    }
}
