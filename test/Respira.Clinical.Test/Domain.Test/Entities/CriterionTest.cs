using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Xunit;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Domain.Test.Entities
{
    public class CriterionTest
    {
        // The same clinical codes used by the real seed data so the criteria below
        // stay clinically meaningful
        private static readonly VariableRef Age = new(Guid.CreateVersion7(), "AGE", ClinicalValueType.Numeric);
        private static readonly VariableRef Sbp = new(Guid.CreateVersion7(), "SBP", ClinicalValueType.Numeric);
        private static readonly VariableRef Smoker = new(Guid.CreateVersion7(), "SMOKER", ClinicalValueType.Boolean);
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

        // AGE >= 65, the CURB-65 age criterion
        private static Criterion AgeCriterion() =>
            new("Tuổi >= 65", new BinaryFormula(
                new VariableFormula(Age),
                new NumericConstantFormula(65),
                ExpressionOperator.Gte));

        // Hút thuốc (smoker), a plain boolean criterion
        private static Criterion SmokerCriterion() =>
            new("Hút thuốc", new VariableFormula(Smoker));

        // AGE >= 65 AND SBP < 90, a criterion that needs two observations
        private static Criterion AgeAndHypotensionCriterion() =>
            new("Tuổi cao và huyết áp thấp", new BinaryFormula(
                new BinaryFormula(new VariableFormula(Age), new NumericConstantFormula(65), ExpressionOperator.Gte),
                new BinaryFormula(new VariableFormula(Sbp), new NumericConstantFormula(90), ExpressionOperator.Lt),
                ExpressionOperator.And));

        // AGE >= 65 OR SBP < 90, a criterion that only needs one of two observations
        private static Criterion AgeOrHypotensionCriterion() =>
            new("Tuổi cao hoặc huyết áp thấp", new BinaryFormula(
                new BinaryFormula(new VariableFormula(Age), new NumericConstantFormula(65), ExpressionOperator.Gte),
                new BinaryFormula(new VariableFormula(Sbp), new NumericConstantFormula(90), ExpressionOperator.Lt),
                ExpressionOperator.Or));

        #region Satisfied

        [Theory]
        [InlineData(65, true)] // boundary: exactly at the threshold
        [InlineData(66, true)] // just above the threshold
        [InlineData(64.99, false)] // just below the threshold
        public void IsCriterionSatisfied_NumericComparison_Evaluates(decimal observedAge, bool expected)
        {
            var criterion = AgeCriterion();

            var result = criterion.IsCriterionSatisfied([Numeric(Age, observedAge)]);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void IsCriterionSatisfied_BooleanVariable_Evaluates(bool observed, bool expected)
        {
            var criterion = SmokerCriterion();

            var result = criterion.IsCriterionSatisfied([Boolean(Smoker, observed)]);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("Klebsiella pneumoniae", true)]
        [InlineData("Streptococcus pneumoniae", false)]
        public void IsCriterionSatisfied_CategoricalComparison_Evaluates(string observed, bool expected)
        {
            var criterion = new Criterion("Tác nhân là Klebsiella", new BinaryFormula(
                new VariableFormula(PathogenCode),
                new CategoricalConstantFormula("Klebsiella pneumoniae"),
                ExpressionOperator.Eq));

            var result = criterion.IsCriterionSatisfied([Categorical(PathogenCode, observed)]);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void IsCriterionSatisfied_DefaultCriterion_ReturnsTrue()
        {
            // The parameterless constructor stores a constant true formula
            var criterion = new Criterion();

            Assert.True(criterion.IsCriterionSatisfied([]));
        }

        [Fact]
        public void IsCriterionSatisfied_ConstantFalseCriterion_ReturnsFalse()
        {
            var criterion = new Criterion("Không bao giờ đúng", new BooleanConstantFormula(false));

            Assert.False(criterion.IsCriterionSatisfied([]));
        }

        [Fact]
        public void IsCriterionSatisfied_OrShortCircuitsWithoutFullObservations_ReturnsTrue()
        {
            // Only AGE is supplied, and it already satisfies the left half, so the
            // missing SBP observation must not make the criterion fail
            var criterion = AgeOrHypotensionCriterion();

            Assert.True(criterion.IsCriterionSatisfied([Numeric(Age, 70)]));
        }

        [Fact]
        public void IsCriterionSatisfied_AndShortCircuitsWithoutFullObservations_ReturnsFalse()
        {
            // Only AGE is supplied, and it already fails the left half, so the
            // missing SBP observation cannot save the criterion
            var criterion = AgeAndHypotensionCriterion();

            Assert.False(criterion.IsCriterionSatisfied([Numeric(Age, 64)]));
        }

        #endregion

        #region Not satisfied

        [Fact]
        public void IsCriterionSatisfied_MissingObservation_ReturnsFalse()
        {
            // A criterion that cannot be evaluated is simply not satisfied
            var criterion = AgeCriterion();

            Assert.False(criterion.IsCriterionSatisfied([]));
        }

        [Fact]
        public void IsCriterionSatisfied_AndNeedsBothObservations_ReturnsFalse()
        {
            // AGE is supplied and meets the threshold, but SBP is missing, so the
            // criterion still cannot be evaluated as satisfied
            var criterion = AgeAndHypotensionCriterion();

            Assert.False(criterion.IsCriterionSatisfied([Numeric(Age, 65)]));
        }

        [Fact]
        public void IsCriterionSatisfied_MalformedFormula_ReturnsFalse()
        {
            // A stored formula that divides by zero throws while the expression is
            // being built; the evaluation must swallow it instead of crashing
            var criterion = new Criterion("Chia cho 0", new BinaryFormula(
                new BinaryFormula(new VariableFormula(Age), new NumericConstantFormula(0), ExpressionOperator.Div),
                new NumericConstantFormula(1),
                ExpressionOperator.Gt));

            Assert.False(criterion.IsCriterionSatisfied([Numeric(Age, 5)]));
        }

        [Fact]
        public void IsCriterionSatisfied_NonBooleanFormula_Throws()
        {
            // The Criterion constructor rejects a non-boolean formula, so the only way
            // to end up with one is assigning the property directly (for instance a
            // corrupt row materialized by EF). That broken state must be reported loudly
            var criterion = new Criterion
            {
                Name = "Công thức không phải boolean",
                Formula = new NumericConstantFormula(65),
            };

            Assert.Throws<Exception>(() => criterion.IsCriterionSatisfied([Numeric(Age, 70)]));
        }

        #endregion
    }
}
