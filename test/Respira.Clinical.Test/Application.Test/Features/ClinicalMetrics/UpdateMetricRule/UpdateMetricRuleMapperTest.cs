using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.ClinicalMetrics.UpdateMetricRule;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Application.Test.Features.ClinicalMetrics.UpdateMetricRule
{
    public class UpdateMetricRuleMapperTest
    {
        private readonly IUpdateMapper<MetricRule, UpdateMetricRuleCommand> _mapper =
            new UpdateMetricRuleMapper(new FormulaMapper());

        // Fixed stamp for the rules before the update, well in the past so the
        // assertion on the new UpdatedAt cannot be flaky
        private static readonly DateTimeOffset OriginalUpdatedAt =
            new(2024, 1, 15, 8, 30, 0, TimeSpan.Zero);

        #region Happy path

        [Fact]
        public void MapModel_ScoringRuleKeepsCriterion_Success()
        {
            /*
             * The natural update: only the score function changes while the rule
             * keeps its own criterion. The mapper just stamps whatever criterion
             * the command carries
             */
            var criterionId = Guid.CreateVersion7();
            var model = CreateScoringRule(criterionId);
            var command = new UpdateMetricRuleCommand
            {
                Id = model.Id,
                ClinicalMetricId = model.ClinicalMetricId,
                CriterionId = criterionId,
                // PSI weight: an elevated blood urea nitrogen is worth 10 points
                ScoreFunction = new FormulaDto { Constant = "10" },
            };

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(criterionId, model.CriterionId);

            // Business rule: the update stamps the rule so its age reflects the change
            Assert.True(model.UpdatedAt > OriginalUpdatedAt);

            var score = Assert.IsType<NumericConstantFormula>(model.ScoreFunction);
            Assert.Equal(10m, score.Constant);

            // The update only rewrites the payload: identity and the soft delete
            // markers of the rule are out of its reach
            Assert.NotEqual(Guid.Empty, model.Id);
            Assert.False(model.IsDeleted);
            Assert.Null(model.DeletedAt);
        }

        [Fact]
        public void MapModel_ScoringRuleTernaryWithVariable_Success()
        {
            /*
             * A score function referencing a clinical variable only resolves when
             * the handler supplies the variables as dependencies: one point for
             * patients at least 65 years old, nothing below
             */
            var age = CreateAgeVariable();
            var model = CreateScoringRule(Guid.CreateVersion7());
            var newCriterionId = Guid.CreateVersion7();
            var command = new UpdateMetricRuleCommand
            {
                Id = model.Id,
                ClinicalMetricId = model.ClinicalMetricId,
                CriterionId = newCriterionId,
                ScoreFunction = AgeBandScore(age.Id),
            };

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable> { age });

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(newCriterionId, model.CriterionId);
            Assert.True(model.UpdatedAt > OriginalUpdatedAt);

            var ternary = Assert.IsType<TernaryFormula>(model.ScoreFunction);
            var condition = Assert.IsType<BinaryFormula>(ternary.Condition);
            Assert.Equal(ExpressionOperator.GTE, condition.Operator);
            Assert.Equal("AGE ≥ 65", condition.ToString());
            Assert.Equal(age.Id, Assert.IsType<VariableFormula>(condition.Left).Variable.Id);
            Assert.Equal(1m, Assert.IsType<NumericConstantFormula>(ternary.IfTrue).Constant);
            Assert.Equal(0m, Assert.IsType<NumericConstantFormula>(ternary.IfFalse).Constant);
        }

        // Boundary values of the IsMajor flag
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MapModel_MajorMinorRule_Success(bool isMajor)
        {
            // IDSA/ATS style: the rule only carries its weight, no score function
            var model = CreateMajorMinorRule(Guid.CreateVersion7(), isMajor: !isMajor);
            var newCriterionId = Guid.CreateVersion7();
            var command = new UpdateMetricRuleCommand
            {
                Id = model.Id,
                ClinicalMetricId = model.ClinicalMetricId,
                CriterionId = newCriterionId,
                IsMajor = isMajor,
            };

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(newCriterionId, model.CriterionId);
            Assert.True(model.UpdatedAt > OriginalUpdatedAt);

            // Business rule: the flag takes the exact boundary value of the command
            Assert.Equal(isMajor, model.IsMajor);
            Assert.False(model.IsDeleted);
            Assert.Null(model.DeletedAt);
        }

        #endregion

        #region Fail path

        [Fact]
        public void MapModel_WithoutDependencies_ThrowsNotImplementedException()
        {
            // The dependency-less overload cannot resolve the formula tree, it only
            // exists to satisfy the mapper interface (the handler always passes the
            // clinical variables loaded from the database)
            var model = CreateScoringRule(Guid.CreateVersion7());
            var command = new UpdateMetricRuleCommand
            {
                Id = model.Id,
                ClinicalMetricId = model.ClinicalMetricId,
                CriterionId = Guid.CreateVersion7(),
                ScoreFunction = new FormulaDto { Constant = "1" },
            };

            Assert.Throws<NotImplementedException>(() => _mapper.MapModel(model, command));
        }

        [Fact]
        public void MapModel_BothScoreFunctionAndIsMajor_ThrowsArgumentException()
        {
            // Business rule: a rule is either a scoring rule or a major/minor rule,
            // never both - the mapper rejects the contradiction itself
            var model = CreateScoringRule(Guid.CreateVersion7());
            var command = new UpdateMetricRuleCommand
            {
                Id = model.Id,
                ClinicalMetricId = model.ClinicalMetricId,
                CriterionId = Guid.CreateVersion7(),
                ScoreFunction = new FormulaDto { Constant = "1" },
                IsMajor = true,
            };

            Assert.Throws<ArgumentException>(
                () => _mapper.MapModel(model, command, new List<ClinicalVariable>()));
        }

        [Fact]
        public void MapModel_NeitherScoreFunctionNorIsMajor_ThrowsArgumentException()
        {
            // Boundary: both nullable fields at their null boundary at the same time
            var model = CreateScoringRule(Guid.CreateVersion7());
            var command = new UpdateMetricRuleCommand
            {
                Id = model.Id,
                ClinicalMetricId = model.ClinicalMetricId,
                CriterionId = Guid.CreateVersion7(),
            };

            Assert.Throws<ArgumentException>(
                () => _mapper.MapModel(model, command, new List<ClinicalVariable>()));
        }

        [Fact]
        public void MapModel_UnknownVariable_Failure()
        {
            // The formula references a clinical variable that is not among the
            // dependencies, so the formula mapping reports a failure that the
            // mapper forwards untouched
            var model = CreateScoringRule(Guid.CreateVersion7());
            var command = new UpdateMetricRuleCommand
            {
                Id = model.Id,
                ClinicalMetricId = model.ClinicalMetricId,
                CriterionId = Guid.CreateVersion7(),
                ScoreFunction = new FormulaDto { Variable = Guid.CreateVersion7() },
            };

            var result = _mapper.MapModel(model, command, new List<ClinicalVariable>());

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        }

        [Fact]
        public void MapModel_NullDependencies_Failure()
        {
            /*
             * Every formula, even a plain constant, is only built when the handler
             * supplied the clinical variables as dependencies
             */
            var model = CreateScoringRule(Guid.CreateVersion7());
            var command = new UpdateMetricRuleCommand
            {
                Id = model.Id,
                ClinicalMetricId = model.ClinicalMetricId,
                CriterionId = Guid.CreateVersion7(),
                ScoreFunction = new FormulaDto { Constant = "1" },
            };

            var result = _mapper.MapModel(model, command, null);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        }

        [Fact]
        public void MapModel_ScoringCommandOnMajorMinorRule_ThrowsInvalidCastException()
        {
            /*
             * The handler refuses a command whose type contradicts the stored rule
             * type before the mapper runs, so this mismatch cannot arrive from
             * production traffic - the mapper itself only casts, which the direct
             * call makes visible as InvalidCastException
             */
            var model = CreateMajorMinorRule(Guid.CreateVersion7(), isMajor: false);
            var command = new UpdateMetricRuleCommand
            {
                Id = model.Id,
                ClinicalMetricId = model.ClinicalMetricId,
                CriterionId = Guid.CreateVersion7(),
                ScoreFunction = new FormulaDto { Constant = "1" },
            };

            Assert.Throws<InvalidCastException>(
                () => _mapper.MapModel(model, command, new List<ClinicalVariable>()));
        }

        [Fact]
        public void MapModel_MajorMinorCommandOnScoringRule_ThrowsInvalidCastException()
        {
            // The other direction of the same type mismatch
            var model = CreateScoringRule(Guid.CreateVersion7());
            var command = new UpdateMetricRuleCommand
            {
                Id = model.Id,
                ClinicalMetricId = model.ClinicalMetricId,
                CriterionId = Guid.CreateVersion7(),
                IsMajor = true,
            };

            Assert.Throws<InvalidCastException>(
                () => _mapper.MapModel(model, command, new List<ClinicalVariable>()));
        }

        #endregion

        private static ScoringRule CreateScoringRule(Guid criterionId) => new()
        {
            ClinicalMetricId = Guid.CreateVersion7(),
            CriterionId = criterionId,
            // CURB-65 style: the criterion is worth exactly one point
            ScoreFunction = new NumericConstantFormula(1),
            UpdatedAt = OriginalUpdatedAt,
        };

        private static MajorMinorRule CreateMajorMinorRule(Guid criterionId, bool isMajor) => new()
        {
            ClinicalMetricId = Guid.CreateVersion7(),
            CriterionId = criterionId,
            IsMajor = isMajor,
            UpdatedAt = OriginalUpdatedAt,
        };

        // One point for patients at least 65 years old, nothing below
        private static FormulaDto AgeBandScore(Guid ageVariableId) => new()
        {
            Condition = new FormulaDto
            {
                Operator = ExpressionOperator.GTE,
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
