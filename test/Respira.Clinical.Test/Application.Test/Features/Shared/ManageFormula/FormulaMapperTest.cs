using System.Globalization;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Application.Test.Features.Shared.ManageFormula
{
    public class FormulaMapperTest
    {
        private readonly FormulaMapper _mapper = new();

        #region Constant mapping

        [Theory]
        [InlineData("true", true)]
        [InlineData("True", true)]
        [InlineData("TRUE", true)]
        [InlineData(" false ", false)]
        public void Map_BooleanConstant_Success(string constant, bool expected)
        {
            var dto = new FormulaDto { Constant = constant };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var formula = Assert.IsType<BooleanConstantFormula>(result.Data);
            Assert.Equal(expected, formula.Constant);
            Assert.Equal(ExpressionResultType.Boolean, formula.ResultType);
        }

        [Theory]
        [InlineData("0", "0")]
        [InlineData("1", "1")]
        [InlineData("-42", "-42")]
        [InlineData("3.5", "3.5")]
        [InlineData(" 42 ", "42")]
        public void Map_NumericConstant_Success(string constant, string expected)
        {
            var dto = new FormulaDto { Constant = constant };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var formula = Assert.IsType<NumericConstantFormula>(result.Data);
            Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), formula.Constant);
            Assert.Equal(ExpressionResultType.Numeric, formula.ResultType);
        }

        [Fact]
        public void Map_DecimalMaxValue_Success()
        {
            var dto = new FormulaDto
            {
                Constant = "79228162514264337593543950335",
            };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var formula = Assert.IsType<NumericConstantFormula>(result.Data);
            Assert.Equal(decimal.MaxValue, formula.Constant);
        }

        [Fact]
        public void Map_NumericOverflow_MapsToCategorical()
        {
            // One past decimal.MaxValue can no longer be parsed as a number,
            // so the constant falls through to the categorical branch
            var dto = new FormulaDto
            {
                Constant = "79228162514264337593543950336",
            };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var formula = Assert.IsType<CategoricalConstantFormula>(result.Data);
            Assert.Equal("79228162514264337593543950336", formula.Constant);
            Assert.Equal(ExpressionResultType.String, formula.ResultType);
        }

        [Theory]
        [InlineData("SEVERE")]
        [InlineData("A+")]
        public void Map_CategoricalConstant_Success(string constant)
        {
            var dto = new FormulaDto { Constant = constant };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);


            var formula = Assert.IsType<CategoricalConstantFormula>(result.Data);
            Assert.Equal(constant, formula.Constant);
            Assert.Equal(ExpressionResultType.String, formula.ResultType);
        }

        [Fact]
        public void Map_EmptyConstant_MapsToCategorical()
        {
            // Empty string rejection lives in FormulaValidator, the mapper maps it as-is
            var dto = new FormulaDto { Constant = string.Empty };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var formula = Assert.IsType<CategoricalConstantFormula>(result.Data);
            Assert.Equal(string.Empty, formula.Constant);
        }

        [Fact]
        public void Map_ConstantResolvedByContent_IgnoresResultType()
        {
            // The declared ResultType is never consulted: a numeric looking constant
            // is mapped to a numeric formula even when the DTO declares String
            var dto = new FormulaDto { Constant = "3.5" };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var formula = Assert.IsType<NumericConstantFormula>(result.Data);
            Assert.Equal(3.5m, formula.Constant);
        }

        #endregion

        #region Variable mapping

        [Fact]
        public void Map_Variable_Success()
        {
            var bun = CreateNumericVariable(Guid.CreateVersion7(), "BUN");
            var dto = new FormulaDto { Variable = bun.Id };

            var result = _mapper.Map(dto, new List<ClinicalVariable> { bun });
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var formula = Assert.IsType<VariableFormula>(result.Data);
            Assert.Equal(bun.Id, formula.Variable.Id);
            Assert.Equal("BUN", formula.Variable.Code);
            Assert.Equal(ClinicalValueType.Numeric, formula.Variable.ValueType);
            Assert.Equal(ExpressionResultType.Numeric, formula.ResultType);
        }

        [Fact]
        public void Map_VariableFoundAmongMultiple_Success()
        {
            var bun = CreateNumericVariable(Guid.CreateVersion7(), "BUN");
            var pregnant = CreateBooleanVariable(Guid.CreateVersion7(), "PREGNANT");
            var dto = new FormulaDto { Variable = pregnant.Id };

            var result = _mapper.Map(dto, new List<ClinicalVariable> { bun, pregnant });
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var formula = Assert.IsType<VariableFormula>(result.Data);
            Assert.Equal(pregnant.Id, formula.Variable.Id);
            Assert.Equal(ExpressionResultType.Boolean, formula.ResultType);
        }

        [Fact]
        public void Map_UnknownVariable_Failure()
        {
            var variableId = Guid.CreateVersion7();
            var dto = new FormulaDto { Variable = variableId };
            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public void Map_UnknownVariableAmongMultiple_Failure()
        {
            var bun = CreateNumericVariable(Guid.CreateVersion7(), "BUN");
            var missingId = Guid.CreateVersion7();
            var dto = new FormulaDto { Variable = missingId };

            var result = _mapper.Map(dto, new List<ClinicalVariable> { bun });

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        #endregion

        #region Binary mapping

        [Fact]
        public void Map_Binary_Success()
        {
            var dto = new FormulaDto
            {
                Operator = ExpressionOperator.ADD,
                Left = new FormulaDto { Constant = "2" },
                Right = new FormulaDto { Constant = "3" },
            };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var formula = Assert.IsType<BinaryFormula>(result.Data);
            Assert.Equal(ExpressionOperator.ADD, formula.Operator);
            Assert.Equal(2m, Assert.IsType<NumericConstantFormula>(formula.Left).Constant);
            Assert.Equal(3m, Assert.IsType<NumericConstantFormula>(formula.Right).Constant);
            Assert.Equal(ExpressionResultType.Numeric, formula.ResultType);
        }

        [Fact]
        public void Map_BinaryWithVariable_Success()
        {
            var bun = CreateNumericVariable(Guid.CreateVersion7(), "BUN");
            var dto = new FormulaDto
            {
                Operator = ExpressionOperator.GT,
                Left = new FormulaDto { Variable = bun.Id },
                Right = new FormulaDto { Constant = "20" },
            };

            var result = _mapper.Map(dto, new List<ClinicalVariable> { bun });
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var formula = Assert.IsType<BinaryFormula>(result.Data);
            var left = Assert.IsType<VariableFormula>(formula.Left);
            Assert.Equal(bun.Id, left.Variable.Id);
            Assert.Equal(20m, Assert.IsType<NumericConstantFormula>(formula.Right).Constant);
            Assert.Contains(formula.Variables, x => x.Id == bun.Id);
            // Comparison operators produce a boolean result even on numeric operands
            Assert.Equal(ExpressionResultType.Boolean, formula.ResultType);
        }

        [Theory]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        public void Map_BinaryMissingPart_Failure(bool includeOperator, bool includeLeft, bool includeRight)
        {
            var dto = new FormulaDto
            {
                Operator = includeOperator ? ExpressionOperator.ADD : null,
                Left = includeLeft ? new FormulaDto { Constant = "1" } : null,
                Right = includeRight ? new FormulaDto { Constant = "2" } : null,
            };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public void Map_BinaryOperandTypeMismatch_Failure()
        {
            var dto = new FormulaDto
            {
                Operator = ExpressionOperator.ADD,
                Left = new FormulaDto { Constant = "true" },
                Right = new FormulaDto { Constant = "1" },
            };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Theory]
        [InlineData("true", "false", ExpressionOperator.ADD)]
        [InlineData("1", "2", ExpressionOperator.AND)]
        [InlineData("A", "B", ExpressionOperator.ADD)]
        public void Map_BinaryOperatorNotAllowedForOperand_Failure(string left, string right, ExpressionOperator op)
        {
            var dto = new FormulaDto
            {
                Operator = op,
                Left = new FormulaDto { Constant = left },
                Right = new FormulaDto { Constant = right },
            };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        #endregion

        #region Unary mapping

        [Fact]
        public void Map_Unary_Success()
        {
            var dto = new FormulaDto
            {
                Operand = new FormulaDto { Constant = "false" },
            };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var formula = Assert.IsType<UnaryFormula>(result.Data);
            var operand = Assert.IsType<BooleanConstantFormula>(formula.Formula);
            Assert.False(operand.Constant);
            Assert.Equal(ExpressionResultType.Boolean, formula.ResultType);
        }

        [Fact]
        public void Map_UnaryNonBooleanOperand_Failure()
        {
            var dto = new FormulaDto
            {
                Operand = new FormulaDto { Constant = "5" },
            };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        #endregion

        #region Ternary mapping

        [Fact]
        public void Map_Ternary_Success()
        {
            var dto = new FormulaDto
            {
                Condition = new FormulaDto { Constant = "true" },
                IfTrue = new FormulaDto { Constant = "1" },
                IfFalse = new FormulaDto { Constant = "2" },
            };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var formula = Assert.IsType<TernaryFormula>(result.Data);
            Assert.IsType<BooleanConstantFormula>(formula.Condition);
            Assert.Equal(1m, Assert.IsType<NumericConstantFormula>(formula.IfTrue).Constant);
            Assert.Equal(2m, Assert.IsType<NumericConstantFormula>(formula.IfFalse).Constant);
        }

        [Fact]
        public void Map_TernaryNonBooleanCondition_Failure()
        {
            var dto = new FormulaDto
            {
                Condition = new FormulaDto { Constant = "1" },
                IfTrue = new FormulaDto { Constant = "1" },
                IfFalse = new FormulaDto { Constant = "2" },
            };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public void Map_TernaryBooleanBranches_Failure()
        {
            var dto = new FormulaDto
            {
                Condition = new FormulaDto { Constant = "true" },
                IfTrue = new FormulaDto { Constant = "true" },
                IfFalse = new FormulaDto { Constant = "false" },
            };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public void Map_TernaryMismatchedBranches_Failure()
        {
            var dto = new FormulaDto
            {
                Condition = new FormulaDto { Constant = "true" },
                IfTrue = new FormulaDto { Constant = "1" },
                IfFalse = new FormulaDto { Constant = "SEVERE" },
            };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public void Map_TernaryMissingIfFalse_Failure()
        {
            var dto = new FormulaDto
            {
                Condition = new FormulaDto { Constant = "true" },
                IfTrue = new FormulaDto { Constant = "1" },
            };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        #endregion

        #region Branch precedence

        [Fact]
        public void Map_ConstantTakesPrecedenceOverVariable_Success()
        {
            var bun = CreateNumericVariable(Guid.CreateVersion7(), "BUN");
            var dto = new FormulaDto { Constant = "5", Variable = bun.Id };

            var result = _mapper.Map(dto, new List<ClinicalVariable> { bun });
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.IsType<NumericConstantFormula>(result.Data);
        }

        [Fact]
        public void Map_VariableTakesPrecedenceOverBinary_Success()
        {
            var bun = CreateNumericVariable(Guid.CreateVersion7(), "BUN");
            var dto = new FormulaDto
            {
                Variable = bun.Id,
                Operator = ExpressionOperator.ADD,
                Left = new FormulaDto { Constant = "1" },
                Right = new FormulaDto { Constant = "2" },
            };

            var result = _mapper.Map(dto, new List<ClinicalVariable> { bun });
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.IsType<VariableFormula>(result.Data);
        }

        [Fact]
        public void Map_BinaryTakesPrecedenceOverUnary_Success()
        {
            var dto = new FormulaDto
            {
                Operator = ExpressionOperator.ADD,
                Left = new FormulaDto { Constant = "1" },
                Right = new FormulaDto { Constant = "2" },
                Operand = new FormulaDto { Constant = "true" },
            };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.IsType<BinaryFormula>(result.Data);
        }

        [Fact]
        public void Map_UnaryTakesPrecedenceOverTernary_Success()
        {
            var dto = new FormulaDto
            {
                Operand = new FormulaDto { Constant = "true" },
                Condition = new FormulaDto { Constant = "true" },
                IfTrue = new FormulaDto { Constant = "1" },
                IfFalse = new FormulaDto { Constant = "2" },
            };

            var result = _mapper.Map(dto, new List<ClinicalVariable>());
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.IsType<UnaryFormula>(result.Data);
        }

        #endregion

        #region Invalid formula and dependencies

        [Fact]
        public void Map_EmptyDto_Failure()
        {
            var result = _mapper.Map(new FormulaDto(), new List<ClinicalVariable>());
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public void Map_NullDependencies_Failure()
        {
            var result = _mapper.Map(new FormulaDto { Constant = "1" }, null);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public void Map_NonVariableListDependencies_Failure()
        {
            var result = _mapper.Map(new FormulaDto { Constant = "1" }, new List<string> { "not", "variables" });
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        #endregion

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
                    Unit = "mg/dL",
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
