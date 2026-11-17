using System.Globalization;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Application.Test.Features.Shared.ManageFormula
{
    public class FormulaModelMapperTest
    {
        private readonly FormulaModelMapper _mapper = new();
        private readonly FormulaDtoMapper _dtoMapper = new();

        // NOTE: the IsFailure() propagation branches of FormulaModelMapper.Map are
        // unreachable in reality - a leaf mapping always succeeds and the only way the
        // method fails is by throwing ArgumentException for an unknown Formula type,
        // which propagates out instead of being converted to a failure Result.
        // Only the throwing path is therefore testable (see the last region).

        #region Constant mapping

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Map_BooleanConstant_Success(bool value)
        {
            var result = _mapper.Map(new BooleanConstantFormula(value));

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            // Business rule: a boolean constant is carried by the Constant string only.
            // bool.ToString() renders "True"/"False" - capitalization is the wire format
            // and FormulaDtoMapper must (case-insensitively) parse it back
            Assert.Equal(value ? "True" : "False", result.Data.Constant);
            AssertOnlyConstantSet(result.Data);
        }

        [Theory]
        // Boundary values of the decimal type with realistic clinical values in between:
        // zero creatinine-like value (lower boundary of a non-negative lab value)
        [InlineData("0", "0")]
        // blood urea nitrogen upper-normal threshold (mg/dL)
        [InlineData("20", "20")]
        // creatinine AKI threshold (mg/dL)
        [InlineData("1.2", "1.2")]
        // HbA1c diabetes diagnosis threshold (%)
        [InlineData("6.5", "6.5")]
        // base excess in metabolic acidosis (mEq/L)
        [InlineData("-2.5", "-2.5")]
        // potassium, hyperkalemia treatment cap (mmol/L)
        [InlineData("4.0", "4.0")]
        public void Map_NumericConstant_RealisticValue_Success(string value, string expected)
        {
            var result = _mapper.Map(new NumericConstantFormula(decimal.Parse(value, CultureInfo.InvariantCulture)));

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            // Business rule: numeric constants are rendered with the invariant culture so
            // the stored/transmitted string is machine-parseable regardless of server locale
            Assert.Equal(expected, result.Data.Constant);
            AssertOnlyConstantSet(result.Data);
        }

        [Fact]
        public void Map_NumericConstant_MaxValue_Success()
        {
            // Upper boundary of decimal: 29 significant digits must survive intact
            var result = _mapper.Map(new NumericConstantFormula(decimal.MaxValue));

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Equal("79228162514264337593543950335", result.Data.Constant);
            AssertOnlyConstantSet(result.Data);
        }

        [Fact]
        public void Map_NumericConstant_MinValue_Success()
        {
            // Lower boundary of decimal: the sign must survive intact
            var result = _mapper.Map(new NumericConstantFormula(decimal.MinValue));

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Equal("-79228162514264337593543950335", result.Data.Constant);
            AssertOnlyConstantSet(result.Data);
        }

        [Theory]
        // These locales render 3.5 as "3,5" under their own culture, so a non-invariant
        // implementation would fail these assertions
        [InlineData("de-DE")]
        [InlineData("fr-FR")]
        [InlineData("vi-VN")]
        public void Map_NumericConstant_IsCultureInvariant_Success(string cultureName)
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

                var result = _mapper.Map(new NumericConstantFormula(3.5m));

                Assert.True(result.IsSuccess());
                Assert.Null(result.Error);
                Assert.Equal(ApplicationStatus.Success, result.StatusCode);
                Assert.NotNull(result.Data);

                Assert.Equal("3.5", result.Data.Constant);
                AssertOnlyConstantSet(result.Data);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [Theory]
        [InlineData("SEVERE")]
        [InlineData("A+")]
        [InlineData("Stage 3B")]
        public void Map_CategoricalConstant_Success(string value)
        {
            var result = _mapper.Map(new CategoricalConstantFormula(value));

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            // Business rule: a categorical constant is carried verbatim, no re-typing
            Assert.Equal(value, result.Data.Constant);
            AssertOnlyConstantSet(result.Data);
        }

        [Fact]
        public void Map_CategoricalConstantLookingNumeric_IsNotRetyped()
        {
            // The DTO representation is derived from the formula's type, not from its
            // content: a categorical constant whose text parses as a number stays a
            // plain string on the wire (retyping would silently change the clinical meaning)
            var result = _mapper.Map(new CategoricalConstantFormula("123"));

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Equal("123", result.Data.Constant);
            AssertOnlyConstantSet(result.Data);
        }

        [Fact]
        public void Map_CategoricalConstantEmptyString_Preserved()
        {
            // Empty-string rejection lives in FormulaValidator, the mapper maps it as-is
            var result = _mapper.Map(new CategoricalConstantFormula(string.Empty));

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Equal(string.Empty, result.Data.Constant);
            AssertOnlyConstantSet(result.Data);
        }

        #endregion

        #region Variable mapping

        [Fact]
        public void Map_NumericVariable_Success()
        {
            var bun = CreateNumericVariable(Guid.CreateVersion7(), "BUN");

            var result = _mapper.Map(new VariableFormula(bun));

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            // Business rule: a variable is carried by its ID only - the consumer
            // resolves the full variable from the ID, the DTO must not embed it
            Assert.Equal(bun.Id, result.Data.Variable);
            AssertOnlyVariableSet(result.Data);
        }

        [Fact]
        public void Map_BooleanVariable_Success()
        {
            var pregnant = CreateBooleanVariable(Guid.CreateVersion7(), "PREGNANT");

            var result = _mapper.Map(new VariableFormula(pregnant));

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Equal(pregnant.Id, result.Data.Variable);
            AssertOnlyVariableSet(result.Data);
        }

        [Fact]
        public void Map_CategoricalVariable_Success()
        {
            var bloodGroup = CreateCategoricalVariable(Guid.CreateVersion7(), "BLOOD_GROUP");

            var result = _mapper.Map(new VariableFormula(bloodGroup));

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Equal(bloodGroup.Id, result.Data.Variable);
            AssertOnlyVariableSet(result.Data);
        }

        #endregion

        #region Binary mapping

        [Theory]
        // All mathematical operators the domain allows on numeric operands
        [InlineData(ExpressionOperator.Add)]
        [InlineData(ExpressionOperator.Sub)]
        [InlineData(ExpressionOperator.Mul)]
        [InlineData(ExpressionOperator.Div)]
        [InlineData(ExpressionOperator.Gt)]
        [InlineData(ExpressionOperator.Lt)]
        [InlineData(ExpressionOperator.Gte)]
        [InlineData(ExpressionOperator.Lte)]
        [InlineData(ExpressionOperator.Eq)]
        [InlineData(ExpressionOperator.Ne)]
        public void Map_BinaryNumericOperator_Success(ExpressionOperator @operator)
        {
            // serum creatinine (mg/dL) vs its AKI threshold (mg/dL)
            var creatinine = CreateNumericVariable(Guid.CreateVersion7(), "CREATININE");
            var formula = new BinaryFormula(
                new VariableFormula(creatinine),
                new NumericConstantFormula(1.2m),
                @operator);

            var result = _mapper.Map(formula);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            // Business rule: a binary node carries its operator plus both recursively
            // mapped operands
            Assert.Equal(@operator, result.Data.Operator);
            Assert.Equal(creatinine.Id, Assert.IsType<FormulaDto>(result.Data.Left).Variable);
            Assert.Equal("1.2", Assert.IsType<FormulaDto>(result.Data.Right).Constant);
            AssertBinaryNodeOnly(result.Data);
        }

        [Theory]
        // All logical operators the domain allows on boolean operands
        [InlineData(ExpressionOperator.And)]
        [InlineData(ExpressionOperator.Or)]
        public void Map_BinaryLogicalOperator_Success(ExpressionOperator @operator)
        {
            var pregnant = CreateBooleanVariable(Guid.CreateVersion7(), "PREGNANT");
            var female = CreateBooleanVariable(Guid.CreateVersion7(), "FEMALE");
            var formula = new BinaryFormula(
                new VariableFormula(pregnant),
                new VariableFormula(female),
                @operator);

            var result = _mapper.Map(formula);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Equal(@operator, result.Data.Operator);
            Assert.Equal(pregnant.Id, Assert.IsType<FormulaDto>(result.Data.Left).Variable);
            Assert.Equal(female.Id, Assert.IsType<FormulaDto>(result.Data.Right).Variable);
            AssertBinaryNodeOnly(result.Data);
        }

        [Fact]
        public void Map_BinaryNestedChildren_Success()
        {
            // (BUN > 20) AND (PREGNANT = false)
            var bun = CreateNumericVariable(Guid.CreateVersion7(), "BUN");
            var pregnant = CreateBooleanVariable(Guid.CreateVersion7(), "PREGNANT");
            var formula = new BinaryFormula(
                new BinaryFormula(
                    new VariableFormula(bun),
                    new NumericConstantFormula(20m),
                    ExpressionOperator.Gt),
                new BinaryFormula(
                    new VariableFormula(pregnant),
                    new BooleanConstantFormula(false),
                    ExpressionOperator.Eq),
                ExpressionOperator.And);

            var result = _mapper.Map(formula);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Equal(ExpressionOperator.And, result.Data.Operator);

            var left = Assert.IsType<FormulaDto>(result.Data.Left);
            Assert.Equal(ExpressionOperator.Gt, left.Operator);
            Assert.Equal(bun.Id, Assert.IsType<FormulaDto>(left.Left).Variable);
            Assert.Equal("20", Assert.IsType<FormulaDto>(left.Right).Constant);

            var right = Assert.IsType<FormulaDto>(result.Data.Right);
            Assert.Equal(ExpressionOperator.Eq, right.Operator);
            Assert.Equal(pregnant.Id, Assert.IsType<FormulaDto>(right.Left).Variable);
            Assert.Equal("False", Assert.IsType<FormulaDto>(right.Right).Constant);
        }

        #endregion

        #region Unary mapping

        [Fact]
        public void Map_Unary_Success()
        {
            var pregnant = CreateBooleanVariable(Guid.CreateVersion7(), "PREGNANT");
            var formula = new UnaryFormula(new VariableFormula(pregnant));

            var result = _mapper.Map(formula);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            // Business rule: negation is implied by the DTO shape (Operand present,
            // Operator absent) - the Not operator itself is not carried on the wire
            Assert.Null(result.Data.Operator);
            Assert.Equal(pregnant.Id, Assert.IsType<FormulaDto>(result.Data.Operand).Variable);
            AssertUnaryNodeOnly(result.Data);
        }

        [Fact]
        public void Map_UnaryNestedOperand_Success()
        {
            // NOT (BUN > 20)
            var bun = CreateNumericVariable(Guid.CreateVersion7(), "BUN");
            var formula = new UnaryFormula(
                new BinaryFormula(
                    new VariableFormula(bun),
                    new NumericConstantFormula(20m),
                    ExpressionOperator.Gt));

            var result = _mapper.Map(formula);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var operand = Assert.IsType<FormulaDto>(result.Data.Operand);
            Assert.Equal(ExpressionOperator.Gt, operand.Operator);
            Assert.Equal(bun.Id, Assert.IsType<FormulaDto>(operand.Left).Variable);
            Assert.Equal("20", Assert.IsType<FormulaDto>(operand.Right).Constant);
            AssertUnaryNodeOnly(result.Data);
        }

        #endregion

        #region Ternary mapping

        [Fact]
        public void Map_Ternary_Success()
        {
            // If eGFR < 30 then 250 else 500 (dose in mg adjusted on kidney function)
            var egfr = CreateNumericVariable(Guid.CreateVersion7(), "EGFR");
            var formula = new TernaryFormula(
                new BinaryFormula(
                    new VariableFormula(egfr),
                    new NumericConstantFormula(30m),
                    ExpressionOperator.Lt),
                new NumericConstantFormula(250m),
                new NumericConstantFormula(500m));

            var result = _mapper.Map(formula);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            // Business rule: a ternary node is identified by the explicit Ternary
            // operator sentinel, alongside its three recursively mapped children
            Assert.Equal(ExpressionOperator.Ternary, result.Data.Operator);

            var condition = Assert.IsType<FormulaDto>(result.Data.Condition);
            Assert.Equal(ExpressionOperator.Lt, condition.Operator);
            Assert.Equal(egfr.Id, Assert.IsType<FormulaDto>(condition.Left).Variable);
            Assert.Equal("30", Assert.IsType<FormulaDto>(condition.Right).Constant);

            Assert.Equal("250", Assert.IsType<FormulaDto>(result.Data.IfTrue).Constant);
            Assert.Equal("500", Assert.IsType<FormulaDto>(result.Data.IfFalse).Constant);
            AssertTernaryNodeOnly(result.Data);
        }

        [Fact]
        public void Map_TernaryVariableBranches_Success()
        {
            // If PREGNANT then BUN else CREATININE (both branches numeric variables)
            var pregnant = CreateBooleanVariable(Guid.CreateVersion7(), "PREGNANT");
            var bun = CreateNumericVariable(Guid.CreateVersion7(), "BUN");
            var creatinine = CreateNumericVariable(Guid.CreateVersion7(), "CREATININE");
            var formula = new TernaryFormula(
                new VariableFormula(pregnant),
                new VariableFormula(bun),
                new VariableFormula(creatinine));

            var result = _mapper.Map(formula);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Equal(ExpressionOperator.Ternary, result.Data.Operator);
            Assert.Equal(pregnant.Id, Assert.IsType<FormulaDto>(result.Data.Condition).Variable);
            Assert.Equal(bun.Id, Assert.IsType<FormulaDto>(result.Data.IfTrue).Variable);
            Assert.Equal(creatinine.Id, Assert.IsType<FormulaDto>(result.Data.IfFalse).Variable);
            AssertTernaryNodeOnly(result.Data);
        }

        #endregion

        #region Nested formula

        [Fact]
        public void Map_DeeplyNestedFormula_Success()
        {
            // (If eGFR < 30 then 250 else 500) > (BUN - 20)
            var egfr = CreateNumericVariable(Guid.CreateVersion7(), "EGFR");
            var bun = CreateNumericVariable(Guid.CreateVersion7(), "BUN");
            var formula = new BinaryFormula(
                new TernaryFormula(
                    new BinaryFormula(
                        new VariableFormula(egfr),
                        new NumericConstantFormula(30m),
                        ExpressionOperator.Lt),
                    new NumericConstantFormula(250m),
                    new NumericConstantFormula(500m)),
                new BinaryFormula(
                    new VariableFormula(bun),
                    new NumericConstantFormula(20m),
                    ExpressionOperator.Sub),
                ExpressionOperator.Gt);

            var result = _mapper.Map(formula);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Equal(ExpressionOperator.Gt, result.Data.Operator);

            var left = Assert.IsType<FormulaDto>(result.Data.Left);
            Assert.Equal(ExpressionOperator.Ternary, left.Operator);
            var condition = Assert.IsType<FormulaDto>(left.Condition);
            Assert.Equal(ExpressionOperator.Lt, condition.Operator);
            Assert.Equal(egfr.Id, Assert.IsType<FormulaDto>(condition.Left).Variable);
            Assert.Equal("30", Assert.IsType<FormulaDto>(condition.Right).Constant);
            Assert.Equal("250", Assert.IsType<FormulaDto>(left.IfTrue).Constant);
            Assert.Equal("500", Assert.IsType<FormulaDto>(left.IfFalse).Constant);

            var right = Assert.IsType<FormulaDto>(result.Data.Right);
            Assert.Equal(ExpressionOperator.Sub, right.Operator);
            Assert.Equal(bun.Id, Assert.IsType<FormulaDto>(right.Left).Variable);
            Assert.Equal("20", Assert.IsType<FormulaDto>(right.Right).Constant);
        }

        #endregion

        #region Round trip to FormulaDto and back

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RoundTrip_BooleanConstant_PreservesFormula(bool value)
        {
            AssertRoundTrip(new BooleanConstantFormula(value), []);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("1.2")]
        [InlineData("6.5")]
        [InlineData("-2.5")]
        [InlineData("4.0")]
        public void RoundTrip_NumericConstant_PreservesFormula(string value)
        {
            AssertRoundTrip(
                new NumericConstantFormula(decimal.Parse(value, CultureInfo.InvariantCulture)),
                []);
        }

        [Fact]
        public void RoundTrip_CategoricalConstant_PreservesFormula()
        {
            AssertRoundTrip(new CategoricalConstantFormula("SEVERE"), []);
        }

        [Fact]
        public void RoundTrip_Variable_PreservesFormula()
        {
            var bun = CreateNumericVariable(Guid.CreateVersion7(), "BUN");

            AssertRoundTrip(new VariableFormula(bun), [bun]);
        }

        [Fact]
        public void RoundTrip_Binary_PreservesFormula()
        {
            var bun = CreateNumericVariable(Guid.CreateVersion7(), "BUN");
            var formula = new BinaryFormula(
                new VariableFormula(bun),
                new NumericConstantFormula(20m),
                ExpressionOperator.Gt);

            AssertRoundTrip(formula, [bun]);
        }

        [Fact]
        public void RoundTrip_Unary_PreservesFormula()
        {
            var pregnant = CreateBooleanVariable(Guid.CreateVersion7(), "PREGNANT");
            var formula = new UnaryFormula(new VariableFormula(pregnant));

            AssertRoundTrip(formula, [pregnant]);
        }

        [Fact]
        public void RoundTrip_Ternary_PreservesFormula()
        {
            var egfr = CreateNumericVariable(Guid.CreateVersion7(), "EGFR");
            var formula = new TernaryFormula(
                new BinaryFormula(
                    new VariableFormula(egfr),
                    new NumericConstantFormula(30m),
                    ExpressionOperator.Lt),
                new NumericConstantFormula(250m),
                new NumericConstantFormula(500m));

            AssertRoundTrip(formula, [egfr]);
        }

        [Fact]
        public void RoundTrip_DeeplyNested_PreservesFormula()
        {
            // (BUN > 20) AND (NOT PREGNANT)
            var bun = CreateNumericVariable(Guid.CreateVersion7(), "BUN");
            var pregnant = CreateBooleanVariable(Guid.CreateVersion7(), "PREGNANT");
            var formula = new BinaryFormula(
                new BinaryFormula(
                    new VariableFormula(bun),
                    new NumericConstantFormula(20m),
                    ExpressionOperator.Gt),
                new UnaryFormula(new VariableFormula(pregnant)),
                ExpressionOperator.And);

            AssertRoundTrip(formula, [bun, pregnant]);
        }

        #endregion

        #region Unknown formula type

        [Fact]
        public void Map_UnknownFormulaType_ThrowsArgumentException()
        {
            // Business rule: the DTO format is a closed set mirroring the seven known
            // formula kinds, so an unmapped Formula subtype must fail fast
            _ = Assert.Throws<ArgumentException>(
                () => _mapper.Map(new UnknownFormula(ExpressionResultType.Numeric)));
        }

        [Fact]
        public void Map_UnknownFormulaTypeNestedInBinary_ThrowsArgumentException()
        {
            // The throw must propagate through the recursive mapping, not be swallowed
            var formula = new BinaryFormula(
                new UnknownFormula(ExpressionResultType.Numeric),
                new NumericConstantFormula(20m),
                ExpressionOperator.Gt);

            _ = Assert.Throws<ArgumentException>(() => _mapper.Map(formula));
        }

        [Fact]
        public void Map_UnknownFormulaTypeNestedInTernary_ThrowsArgumentException()
        {
            // Ternary requires a boolean condition, so the unknown subtype must report
            // a boolean result type to even reach the recursive mapping
            var formula = new TernaryFormula(
                new UnknownFormula(ExpressionResultType.Boolean),
                new NumericConstantFormula(250m),
                new NumericConstantFormula(500m));

            _ = Assert.Throws<ArgumentException>(() => _mapper.Map(formula));
        }

        #endregion

        #region Test helpers

        /// <summary>
        /// Business rule assertion: a leaf DTO (constant or variable) must carry its
        /// single distinguishing field and nothing else.
        /// </summary>
        private static void AssertOnlyConstantSet(FormulaDto dto)
        {
            Assert.Null(dto.Variable);
            Assert.Null(dto.Operator);
            Assert.Null(dto.Left);
            Assert.Null(dto.Right);
            Assert.Null(dto.Operand);
            Assert.Null(dto.Condition);
            Assert.Null(dto.IfTrue);
            Assert.Null(dto.IfFalse);
        }

        private static void AssertOnlyVariableSet(FormulaDto dto)
        {
            Assert.Null(dto.Constant);
            Assert.Null(dto.Operator);
            Assert.Null(dto.Left);
            Assert.Null(dto.Right);
            Assert.Null(dto.Operand);
            Assert.Null(dto.Condition);
            Assert.Null(dto.IfTrue);
            Assert.Null(dto.IfFalse);
        }

        /// <summary>
        /// Business rule assertion: a binary DTO must carry Operator + Left + Right and
        /// none of the unary/ternary-only fields.
        /// </summary>
        private static void AssertBinaryNodeOnly(FormulaDto dto)
        {
            Assert.NotNull(dto.Operator);
            Assert.NotNull(dto.Left);
            Assert.NotNull(dto.Right);
            Assert.Null(dto.Constant);
            Assert.Null(dto.Variable);
            Assert.Null(dto.Operand);
            Assert.Null(dto.Condition);
            Assert.Null(dto.IfTrue);
            Assert.Null(dto.IfFalse);
        }

        /// <summary>
        /// Business rule assertion: a unary DTO must carry Operand only - no Operator
        /// (negation is implied by the shape), no constant/variable of its own.
        /// </summary>
        private static void AssertUnaryNodeOnly(FormulaDto dto)
        {
            Assert.NotNull(dto.Operand);
            Assert.Null(dto.Operator);
            Assert.Null(dto.Constant);
            Assert.Null(dto.Variable);
            Assert.Null(dto.Left);
            Assert.Null(dto.Right);
            Assert.Null(dto.Condition);
            Assert.Null(dto.IfTrue);
            Assert.Null(dto.IfFalse);
        }

        /// <summary>
        /// Business rule assertion: a ternary DTO must carry the Ternary operator
        /// sentinel + Condition/IfTrue/IfFalse, and none of the binary-only fields.
        /// </summary>
        private static void AssertTernaryNodeOnly(FormulaDto dto)
        {
            Assert.Equal(ExpressionOperator.Ternary, dto.Operator);
            Assert.NotNull(dto.Condition);
            Assert.NotNull(dto.IfTrue);
            Assert.NotNull(dto.IfFalse);
            Assert.Null(dto.Constant);
            Assert.Null(dto.Variable);
            Assert.Null(dto.Left);
            Assert.Null(dto.Right);
            Assert.Null(dto.Operand);
        }

        /// <summary>
        /// Business rule: the mapped DTO must be re-ingestable by <see cref="FormulaDtoMapper"/>
        /// - mapping Model -> DTO -> Model must reproduce the same formula tree.
        /// The formula tree is compared through its unambiguous string rendering.
        /// </summary>
        private void AssertRoundTrip(Formula formula, List<ClinicalVariable> variables)
        {
            var toDtoResult = _mapper.Map(formula);
            Assert.True(toDtoResult.IsSuccess());
            Assert.NotNull(toDtoResult.Data);

            var backResult = _dtoMapper.Map(toDtoResult.Data, variables);
            Assert.True(backResult.IsSuccess());
            Assert.NotNull(backResult.Data);

            Assert.Equal(formula.ToString(), backResult.Data.ToString());
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
                    Max = 150,
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

        private static CategoricalClinicalVariable CreateCategoricalVariable(Guid id, string code)
        {
            return new CategoricalClinicalVariable
            {
                Id = id,
                Code = code,
                Name = code,
                Description = $"{code} clinical variable",
                IsRequired = false,
                Category = ClinicalVariableCategory.PersonalInformation,
                AcceptedValues = ["A+", "B+", "O-", "AB-"]
            };
        }

        /// <summary>
        /// A Formula subtype outside the closed set the mapper knows how to serialize.
        /// </summary>
        private sealed class UnknownFormula(ExpressionResultType resultType) : Formula
        {
            public override ExpressionResultType ResultType => resultType;

            public override Expression ToExpression(IEnumerable<ClinicalObservation> observations)
            {
                throw new NotSupportedException();
            }

            public override IEnumerable<VariableRef> Variables => [];
        }

        #endregion
    }
}
