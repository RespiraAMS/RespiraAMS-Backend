using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.ClinicalMetrics.AddMetricRule;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Application.Test.Features.ClinicalMetrics.AddMetricRule
{
    public class AddMetricRuleMapperTest
    {
        private readonly ICreateMapper<AddMetricRuleCommand, MetricRule> _mapper =
            new AddMetricRuleMapper(new FormulaMapper());

        #region Map command to model

        [Fact]
        public void ToModel_ScoringRuleConstant_Success()
        {
            // CURB-65 style: every criterion of the score is worth exactly one point
            var command = ScoringCommand(new FormulaDto { Constant = "1" });

            var result = _mapper.ToModel(command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var rule = Assert.IsType<ScoringRule>(result.Data);
            Assert.Equal(command.ClinicalMetricId, rule.ClinicalMetricId);
            Assert.Equal(command.CriterionId, rule.CriterionId);
            Assert.False(rule.IsDeleted);
            Assert.Null(rule.DeletedAt);

            var score = Assert.IsType<NumericConstantFormula>(rule.ScoreFunction);
            Assert.Equal(1m, score.Constant);
        }

        [Fact]
        public void ToModel_ScoringRuleTernaryWithVariable_Success()
        {
            /*
             * PSI style: a score function referencing a clinical variable through a
             * nested formula tree, resolved against the dependencies the handler
             * loads from the database
             */
            var age = CreateAgeVariable();
            var command = ScoringCommand(AgeBandScore(age.Id));

            var result = _mapper.ToModel(command, new List<ClinicalVariable> { age });

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var rule = Assert.IsType<ScoringRule>(result.Data);
            Assert.Equal(command.ClinicalMetricId, rule.ClinicalMetricId);
            Assert.Equal(command.CriterionId, rule.CriterionId);

            var ternary = Assert.IsType<TernaryFormula>(rule.ScoreFunction);
            var condition = Assert.IsType<BinaryFormula>(ternary.Condition);
            Assert.Equal(ExpressionOperator.Gte, condition.Operator);
            Assert.Equal("AGE ≥ 65", condition.ToString());
            Assert.Equal(age.Id, Assert.IsType<VariableFormula>(condition.Left).Variable.Id);
            Assert.Equal(1m, Assert.IsType<NumericConstantFormula>(ternary.IfTrue).Constant);
            Assert.Equal(0m, Assert.IsType<NumericConstantFormula>(ternary.IfFalse).Constant);
        }

        // Boundary values of the IsMajor flag
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ToModel_MajorMinorRule_Success(bool isMajor)
        {
            // IDSA/ATS style: the rule only carries its weight, no score function
            var command = MajorMinorCommand(isMajor);

            var result = _mapper.ToModel(command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var rule = Assert.IsType<MajorMinorRule>(result.Data);
            Assert.Equal(command.ClinicalMetricId, rule.ClinicalMetricId);
            Assert.Equal(command.CriterionId, rule.CriterionId);
            Assert.Equal(isMajor, rule.IsMajor);
            Assert.False(rule.IsDeleted);
            Assert.Null(rule.DeletedAt);
        }

        #endregion

        #region Fail path

        [Fact]
        public void ToModel_WithoutDependencies_ThrowsNotImplementedException()
        {
            // The dependency-less overload cannot resolve the formula tree, it only
            // exists to satisfy the mapper interface (the handler always passes the
            // clinical variables loaded from the database)
            var command = ScoringCommand(new FormulaDto { Constant = "1" });

            Assert.Throws<NotImplementedException>(() => _mapper.ToModel(command));
        }

        [Fact]
        public void ToModel_BothScoreFunctionAndIsMajor_ThrowsArgumentException()
        {
            // Business rule: a rule is either a scoring rule or a major/minor rule,
            // never both - the mapper rejects the contradiction itself
            var command = ScoringCommand(new FormulaDto { Constant = "1" });
            command.IsMajor = true;

            Assert.Throws<ArgumentException>(
                () => _mapper.ToModel(command, new List<ClinicalVariable>()));
        }

        [Fact]
        public void ToModel_NeitherScoreFunctionNorIsMajor_ThrowsArgumentException()
        {
            // Boundary: both nullable fields at their null boundary at the same time
            var command = new AddMetricRuleCommand
            {
                ClinicalMetricId = Guid.CreateVersion7(),
                CriterionId = Guid.CreateVersion7(),
            };

            Assert.Throws<ArgumentException>(
                () => _mapper.ToModel(command, new List<ClinicalVariable>()));
        }

        [Fact]
        public void ToModel_UnknownVariable_Failure()
        {
            // The formula references a clinical variable that is not among the
            // dependencies, so the formula mapping reports a failure that the
            // mapper forwards untouched
            var command = ScoringCommand(new FormulaDto { Variable = Guid.CreateVersion7() });

            var result = _mapper.ToModel(command, new List<ClinicalVariable>());

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);
        }

        #endregion

        private static AddMetricRuleCommand ScoringCommand(FormulaDto scoreFunction) => new()
        {
            ClinicalMetricId = Guid.CreateVersion7(),
            CriterionId = Guid.CreateVersion7(),
            ScoreFunction = scoreFunction,
        };

        private static AddMetricRuleCommand MajorMinorCommand(bool isMajor) => new()
        {
            ClinicalMetricId = Guid.CreateVersion7(),
            CriterionId = Guid.CreateVersion7(),
            IsMajor = isMajor,
        };

        // One point for patients at least 65 years old, nothing below
        private static FormulaDto AgeBandScore(Guid ageVariableId) => new()
        {
            Condition = new FormulaDto
            {
                Operator = ExpressionOperator.Gte,
                Left = new FormulaDto { Variable = ageVariableId },
                Right = new FormulaDto { Constant = "65" },
            },
            IfTrue = new FormulaDto { Constant = "1" },
            IfFalse = new FormulaDto { Constant = "0" },
        };

        // Patient age, the CURB-65 severity threshold sits at 65 years
        private static NumericClinicalVariable CreateAgeVariable() => new()
        {
            Code = "AGE",
            Name = "Tuổi",
            Description = "Patient age in years",
            CanonicalUnit = "year",
            IsRequired = true,
            Category = ClinicalVariableCategory.Clinical,
            AcceptedRange = new Range
            {
                Min = 0,
                IsMinExclusive = false,
                Max = 120,
                IsMaxExclusive = false,
                Unit = "year",
            },
        };
    }
}
