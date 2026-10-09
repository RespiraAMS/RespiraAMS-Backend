using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Xunit;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Domain.Test.Models
{
    public class FormulaTest
    {
        // The variables every test in this file works with: the same codes used by the
        // real seed data (CURB-65 style) so the formulas below stay clinically meaningful
        private static readonly VariableRef Age = new(Guid.CreateVersion7(), "AGE", ClinicalValueType.Numeric);
        private static readonly VariableRef Sbp = new(Guid.CreateVersion7(), "SBP", ClinicalValueType.Numeric);
        private static readonly VariableRef Smoker = new(Guid.CreateVersion7(), "SMOKER", ClinicalValueType.Boolean);
        private static readonly VariableRef Pregnant = new(Guid.CreateVersion7(), "PREGNANT", ClinicalValueType.Boolean);
        private static readonly VariableRef PathogenCode = new(Guid.CreateVersion7(), "PATHOGEN", ClinicalValueType.Categorical);

        private static ClinicalVariable ObservationVariable(VariableRef variable) => variable.ValueType switch
        {
            ClinicalValueType.Numeric => new NumericClinicalVariable
            {
                Code = variable.Code,
                Name = variable.Code,
                Description = $"Observed clinical value of {variable.Code}",
                IsRequired = false,
                Category = ClinicalVariableCategory.Clinical,
                AcceptedRange = new Range
                {
                    Min = 0,
                    IsMinExclusive = false,
                    Max = 250,
                    IsMaxExclusive = false,
                    Unit = null,
                },
            },
            ClinicalValueType.Boolean => new BooleanClinicalVariable
            {
                Code = variable.Code,
                Name = variable.Code,
                Description = $"Observed clinical value of {variable.Code}",
                IsRequired = false,
                Category = ClinicalVariableCategory.Clinical,
            },
            _ => new CategoricalClinicalVariable([])
            {
                Code = variable.Code,
                Name = variable.Code,
                Description = $"Observed clinical value of {variable.Code}",
                IsRequired = false,
                Category = ClinicalVariableCategory.Clinical,
            },
        };

        private static ClinicalObservation Numeric(VariableRef variable, decimal value) =>
            new(ObservationVariable(variable), value);

        private static ClinicalObservation Boolean(VariableRef variable, bool value) =>
            new(ObservationVariable(variable), value);

        private static ClinicalObservation Categorical(VariableRef variable, string value) =>
            new(ObservationVariable(variable), value);

        #region BinaryFormula.ToExpression

        [Theory]
        [InlineData(ExpressionOperator.GTE, 65, true)] // at the threshold: AGE >= 65 holds
        [InlineData(ExpressionOperator.GTE, 64.99, false)] // just below the threshold
        [InlineData(ExpressionOperator.GT, 66, true)] // just above the threshold
        [InlineData(ExpressionOperator.GT, 65, false)] // strictly greater than fails at the threshold
        [InlineData(ExpressionOperator.LT, 64.99, true)]
        [InlineData(ExpressionOperator.LTE, 65, true)] // at the threshold still holds
        [InlineData(ExpressionOperator.EQ, 65, true)]
        [InlineData(ExpressionOperator.NE, 65, false)]
        public void BinaryFormula_ToExpression_NumericComparison_Evaluates(
            ExpressionOperator op, decimal observedAge, bool expected)
        {
            var formula = new BinaryFormula(
                new VariableFormula(Age),
                new NumericConstantFormula(65),
                op);

            var expression = formula.ToExpression([Numeric(Age, observedAge)]);

            Assert.Equal(ExpressionResultType.Boolean, expression.ResultType);
            Assert.Equal(expected, expression.Evaluate());
        }

        [Theory]
        [InlineData(60, 65)] // 60 + 5 lands exactly on the 65 threshold
        [InlineData(59.99, 64.99)]
        [InlineData(0, 5)] // lower boundary of the observed value
        [InlineData(250, 255)] // upper boundary of the observed value
        public void BinaryFormula_ToExpression_Arithmetic_Evaluates(decimal observedAge, decimal expected)
        {
            var formula = new BinaryFormula(
                new VariableFormula(Age),
                new NumericConstantFormula(5),
                ExpressionOperator.ADD);

            var expression = formula.ToExpression([Numeric(Age, observedAge)]);

            Assert.Equal(ExpressionResultType.Numeric, expression.ResultType);
            Assert.Equal(expected, expression.Evaluate());
        }

        [Theory]
        [InlineData(ExpressionOperator.GT, 65, 90, false)]
        [InlineData(ExpressionOperator.GTE, 90, 90, true)] // both boundaries equal
        [InlineData(ExpressionOperator.LT, 65, 90, true)]
        [InlineData(ExpressionOperator.LTE, 90, 90, true)] // both boundaries equal
        [InlineData(ExpressionOperator.EQ, 90, 90, true)]
        [InlineData(ExpressionOperator.NE, 65, 90, true)]
        public void BinaryFormula_ToExpression_ConstantOperands_EvaluatesWithoutObservations(
            ExpressionOperator op, decimal left, decimal right, bool expected)
        {
            var formula = new BinaryFormula(
                new NumericConstantFormula(left),
                new NumericConstantFormula(right),
                op);

            // No observation is ever required for a constant-only formula
            var expression = formula.ToExpression([]);

            Assert.Equal(ExpressionResultType.Boolean, expression.ResultType);
            Assert.Equal(expected, expression.Evaluate());
        }

        [Theory]
        [InlineData("Klebsiella pneumoniae", "Klebsiella pneumoniae", ExpressionOperator.EQ, true)]
        [InlineData("Klebsiella pneumoniae", "Streptococcus pneumoniae", ExpressionOperator.EQ, false)]
        [InlineData("Klebsiella pneumoniae", "Streptococcus pneumoniae", ExpressionOperator.NE, true)]
        public void BinaryFormula_ToExpression_CategoricalComparison_Evaluates(
            string observed, string constant, ExpressionOperator op, bool expected)
        {
            var formula = new BinaryFormula(
                new VariableFormula(PathogenCode),
                new CategoricalConstantFormula(constant),
                op);

            var expression = formula.ToExpression([Categorical(PathogenCode, observed)]);

            Assert.Equal(ExpressionResultType.Boolean, expression.ResultType);
            Assert.Equal(expected, expression.Evaluate());
        }

        public static TheoryData<decimal, bool> NestedFormula_Age =
        [
            (60m, true), // (60 + 5) >= 65 -> true at the threshold
            (59.99m, false), // (59.99 + 5) >= 65 -> false just below the threshold
        ];

        [Theory]
        [MemberData(nameof(NestedFormula_Age))]
        public void BinaryFormula_ToExpression_NestedOperands_Evaluates(decimal observedAge, bool expected)
        {
            // (AGE + 5) >= 65
            var formula = new BinaryFormula(
                new BinaryFormula(
                    new VariableFormula(Age),
                    new NumericConstantFormula(5),
                    ExpressionOperator.ADD),
                new NumericConstantFormula(65),
                ExpressionOperator.GTE);

            var expression = formula.ToExpression([Numeric(Age, observedAge)]);

            Assert.Equal(ExpressionResultType.Boolean, expression.ResultType);
            Assert.Equal(expected, expression.Evaluate());
        }

        [Theory]
        [InlineData(65, 89, true)] // both boundaries of AGE >= 65 AND SBP < 90 are met
        [InlineData(64.99, 89, false)] // only the age half fails
        [InlineData(65, 90, false)] // only the hypotension half fails (SBP < 90 is strict)
        [InlineData(64.99, 90, false)] // both halves fail
        public void BinaryFormula_ToExpression_WithBothObservations_Evaluates(
            decimal observedAge, decimal observedSbp, bool expected)
        {
            // AGE >= 65 AND SBP < 90
            var formula = new BinaryFormula(
                new BinaryFormula(new VariableFormula(Age), new NumericConstantFormula(65), ExpressionOperator.GTE),
                new BinaryFormula(new VariableFormula(Sbp), new NumericConstantFormula(90), ExpressionOperator.LT),
                ExpressionOperator.AND);

            var expression = formula.ToExpression(
                [Numeric(Age, observedAge), Numeric(Sbp, observedSbp)]);

            Assert.Equal(ExpressionResultType.Boolean, expression.ResultType);
            Assert.Equal(expected, expression.Evaluate());
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public void BinaryFormula_ToExpression_And_Evaluates(bool left, bool right, bool expected)
        {
            var formula = new BinaryFormula(
                new VariableFormula(Smoker),
                new VariableFormula(Pregnant),
                ExpressionOperator.AND);

            var expression = formula.ToExpression(
                [Boolean(Smoker, left), Boolean(Pregnant, right)]);

            Assert.Equal(ExpressionResultType.Boolean, expression.ResultType);
            Assert.Equal(expected, expression.Evaluate());
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(false, false, false)]
        public void BinaryFormula_ToExpression_Or_Evaluates(bool left, bool right, bool expected)
        {
            var formula = new BinaryFormula(
                new VariableFormula(Smoker),
                new VariableFormula(Pregnant),
                ExpressionOperator.OR);

            var expression = formula.ToExpression(
                [Boolean(Smoker, left), Boolean(Pregnant, right)]);

            Assert.Equal(ExpressionResultType.Boolean, expression.ResultType);
            Assert.Equal(expected, expression.Evaluate());
        }

        [Fact]
        public void BinaryFormula_ToExpression_AndFalseLeft_ObservationOfRightNotRequired()
        {
            // false AND <unknown> is false whatever the right operand is, so the
            // observation for the right variable must not be needed to build the expression
            var formula = new BinaryFormula(
                new BooleanConstantFormula(false),
                new VariableFormula(Pregnant),
                ExpressionOperator.AND);

            var expression = formula.ToExpression([]);

            Assert.Equal(ExpressionResultType.Boolean, expression.ResultType);
            Assert.Equal(false, expression.Evaluate());
        }

        [Fact]
        public void BinaryFormula_ToExpression_AndObservedFalseLeft_ObservationOfRightNotRequired()
        {
            // Same short-circuit, but the false comes from an observation instead of a constant
            var formula = new BinaryFormula(
                new VariableFormula(Smoker),
                new VariableFormula(Pregnant),
                ExpressionOperator.AND);

            var expression = formula.ToExpression([Boolean(Smoker, false)]);

            Assert.Equal(ExpressionResultType.Boolean, expression.ResultType);
            Assert.Equal(false, expression.Evaluate());
        }

        [Fact]
        public void BinaryFormula_ToExpression_OrTrueLeft_ObservationOfRightNotRequired()
        {
            // true OR <unknown> is true whatever the right operand is
            var formula = new BinaryFormula(
                new VariableFormula(Smoker),
                new VariableFormula(Pregnant),
                ExpressionOperator.OR);

            var expression = formula.ToExpression([Boolean(Smoker, true)]);

            Assert.Equal(ExpressionResultType.Boolean, expression.ResultType);
            Assert.Equal(true, expression.Evaluate());
        }

        [Fact]
        public void BinaryFormula_ToExpression_AndTrueLeftMissingRightObservation_Fail()
        {
            // true AND <unknown> cannot be decided, so the missing observation must surface
            var formula = new BinaryFormula(
                new VariableFormula(Smoker),
                new VariableFormula(Pregnant),
                ExpressionOperator.AND);

            Assert.Throws<ArgumentException>(() =>
                formula.ToExpression([Boolean(Smoker, true)]));
        }

        [Fact]
        public void BinaryFormula_ToExpression_OrFalseLeftMissingRightObservation_Fail()
        {
            // false OR <unknown> cannot be decided either
            var formula = new BinaryFormula(
                new VariableFormula(Smoker),
                new VariableFormula(Pregnant),
                ExpressionOperator.OR);

            Assert.Throws<ArgumentException>(() =>
                formula.ToExpression([Boolean(Smoker, false)]));
        }

        [Fact]
        public void BinaryFormula_ToExpression_AndBothObservationsMissing_Fail()
        {
            var formula = new BinaryFormula(
                new VariableFormula(Smoker),
                new VariableFormula(Pregnant),
                ExpressionOperator.AND);

            Assert.Throws<ArgumentException>(() => formula.ToExpression([]));
        }

        [Fact]
        public void BinaryFormula_ToExpression_MissingObservation_Fail()
        {
            // Operators other than AND/OR never short-circuit, so the observation
            // is mandatory to even build the expression
            var formula = new BinaryFormula(
                new VariableFormula(Age),
                new NumericConstantFormula(65),
                ExpressionOperator.GTE);

            Assert.Throws<ArgumentException>(() => formula.ToExpression([]));
        }

        #endregion

        #region TernaryFormula.ToExpression

        [Theory]
        [InlineData(true, 65)]
        [InlineData(false, 75)]
        public void TernaryFormula_ToExpression_ConditionConstant_EvaluatesBranch(
            bool conditionValue, decimal expected)
        {
            var formula = new TernaryFormula(
                new BooleanConstantFormula(conditionValue),
                new NumericConstantFormula(65),
                new NumericConstantFormula(75));

            var expression = formula.ToExpression([]);

            Assert.Equal(ExpressionResultType.Numeric, expression.ResultType);
            Assert.Equal(expected, expression.Evaluate());
        }

        [Theory]
        [InlineData(70, 1)] // AGE >= 65 -> the score of the true branch
        [InlineData(65, 1)] // boundary: exactly 65 still takes the true branch
        [InlineData(64.99, 0)] // just below the boundary takes the false branch
        public void TernaryFormula_ToExpression_ConditionFromObservation_EvaluatesBranch(
            decimal observedAge, decimal expected)
        {
            // If AGE >= 65 then 1 else 0, the shape used by the CURB-65 scoring rules
            var formula = new TernaryFormula(
                new BinaryFormula(new VariableFormula(Age), new NumericConstantFormula(65), ExpressionOperator.GTE),
                new NumericConstantFormula(1),
                new NumericConstantFormula(0));

            var expression = formula.ToExpression([Numeric(Age, observedAge)]);

            Assert.Equal(ExpressionResultType.Numeric, expression.ResultType);
            Assert.Equal(expected, expression.Evaluate());
        }

        [Fact]
        public void TernaryFormula_ToExpression_UnreachableFalseBranchObservationNotRequired()
        {
            // The condition is true, so the false branch is never reached and its
            // missing SBP observation must not stop the expression from being built
            var formula = new TernaryFormula(
                new BooleanConstantFormula(true),
                new BinaryFormula(new VariableFormula(Age), new NumericConstantFormula(0), ExpressionOperator.ADD),
                new BinaryFormula(new VariableFormula(Sbp), new NumericConstantFormula(0), ExpressionOperator.ADD));

            var expression = formula.ToExpression([Numeric(Age, 70)]);

            Assert.Equal(ExpressionResultType.Numeric, expression.ResultType);
            Assert.Equal(70m, expression.Evaluate());
        }

        [Fact]
        public void TernaryFormula_ToExpression_UnreachableTrueBranchObservationNotRequired()
        {
            // Mirror image: the condition is false, so the true branch is never reached
            // and its missing AGE observation must not stop the expression from being built
            var formula = new TernaryFormula(
                new BooleanConstantFormula(false),
                new BinaryFormula(new VariableFormula(Age), new NumericConstantFormula(0), ExpressionOperator.ADD),
                new BinaryFormula(new VariableFormula(Sbp), new NumericConstantFormula(0), ExpressionOperator.ADD));

            var expression = formula.ToExpression([Numeric(Sbp, 89)]);

            Assert.Equal(ExpressionResultType.Numeric, expression.ResultType);
            Assert.Equal(89m, expression.Evaluate());
        }

        [Fact]
        public void TernaryFormula_ToExpression_MissingConditionObservation_Fail()
        {
            var formula = new TernaryFormula(
                new BinaryFormula(new VariableFormula(Age), new NumericConstantFormula(65), ExpressionOperator.GTE),
                new NumericConstantFormula(1),
                new NumericConstantFormula(0));

            Assert.Throws<ArgumentException>(() => formula.ToExpression([]));
        }

        [Fact]
        public void TernaryFormula_ToExpression_MissingReachableBranchObservation_Fail()
        {
            // The condition is true but the reachable branch needs an AGE observation
            // that was not supplied, so the expression cannot be built
            var formula = new TernaryFormula(
                new BooleanConstantFormula(true),
                new BinaryFormula(new VariableFormula(Age), new NumericConstantFormula(0), ExpressionOperator.ADD),
                new NumericConstantFormula(0));

            Assert.Throws<ArgumentException>(() => formula.ToExpression([]));
        }

        [Fact]
        public void TernaryFormula_ToExpression_CategoricalBranch_Evaluates()
        {
            // A ternary whose branches are categorical: TernaryFormula.ResultType
            // reports String, so the built expression must evaluate back to the branch value
            var formula = new TernaryFormula(
                new BooleanConstantFormula(true),
                new CategoricalConstantFormula("KLEBSIELLA_PNEUMONIAE"),
                new CategoricalConstantFormula("STREPTOCOCCUS_PNEUMONIAE"));

            var expression = formula.ToExpression([]);

            Assert.Equal(ExpressionResultType.String, expression.ResultType);
            Assert.Equal("KLEBSIELLA_PNEUMONIAE", expression.Evaluate());
        }

        #endregion
    }
}
