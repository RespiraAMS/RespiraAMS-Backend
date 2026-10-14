using Respira.Clinical.Application.Features.ClinicalMetrics.AddMetricRule;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Enums;

namespace Respira.Clinical.Application.Test.Features.ClinicalMetrics.AddMetricRule
{
    public class AddMetricRuleValidatorTest
    {
        private readonly AddMetricRuleValidator _validator = new();

        #region Valid command

        [Fact]
        public async Task AddRule_ScoringRuleConstant_Success()
        {
            // Business rule: a scoring rule carries a score function and no
            // major/minor flag
            var command = ScoringCommand(new FormulaDto { Constant = "1" });

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task AddRule_ScoringRuleVariable_Success()
        {
            // A score function may be a bare clinical variable reference
            var command = ScoringCommand(new FormulaDto { Variable = Guid.CreateVersion7() });

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task AddRule_ScoringRuleNestedTernary_Success()
        {
            /*
             * The deepest shape the recursive FormulaValidator has to walk: a ternary
             * whose condition is a binary comparison on a variable, like the age band
             * score of a real severity metric
             */
            var command = ScoringCommand(AgeBandScore(Guid.CreateVersion7()));

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Boundary values of the IsMajor flag
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task AddRule_MajorMinorRule_Success(bool isMajor)
        {
            // Business rule: a major/minor rule carries the flag and no score function
            var command = MajorMinorCommand(isMajor);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Boundary: a UUID v7 is the usual shape, plus the lowest and highest
        // non-empty GUID values that NotEmpty still accepts
        public static readonly TheoryData<Guid> ValidIds =
        [
            Guid.CreateVersion7(),
            new Guid("00000000-0000-0000-0000-000000000001"),
            new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
        ];

        [Theory]
        [MemberData(nameof(ValidIds))]
        public async Task AddRule_ValidIds_Success(Guid id)
        {
            var command = ScoringCommand(new FormulaDto { Constant = "1" }) with
            {
                ClinicalMetricId = id,
                CriterionId = id,
            };

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        [Fact]
        public async Task AddRule_EmptyClinicalMetricId_Fail()
        {
            // Boundary: Guid.Empty is the only value rejected by NotEmpty on a Guid
            var command = ScoringCommand(new FormulaDto { Constant = "1" });
            command.ClinicalMetricId = Guid.Empty;

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("ClinicalMetricId", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task AddRule_EmptyCriterionId_Fail()
        {
            var command = ScoringCommand(new FormulaDto { Constant = "1" });
            command.CriterionId = Guid.Empty;

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("CriterionId", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task AddRule_BothScoreFunctionAndIsMajor_Fail()
        {
            /*
             * Business rule: exactly one of score function / major-minor flag may be
             * set, so a command carrying both is rejected as a whole
             */
            var command = ScoringCommand(new FormulaDto { Constant = "1" });
            command.IsMajor = true;

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            // Only the exclusivity rule fires, the supplied formula itself is valid
            _ = Assert.Single(result.Errors);
            Assert.Equal(string.Empty, result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task AddRule_NeitherScoreFunctionNorIsMajor_Fail()
        {
            // Boundary: both nullable fields at their null boundary at the same time
            var command = new AddMetricRuleCommand
            {
                ClinicalMetricId = Guid.CreateVersion7(),
                CriterionId = Guid.CreateVersion7(),
            };

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(string.Empty, result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task AddRule_EmptyScoreFunction_Fail()
        {
            // An empty formula object has none of the fields a formula needs
            var command = ScoringCommand(new FormulaDto());

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            // The exclusivity rule passes (a score function was supplied), only the
            // delegated FormulaValidator rejects the shape
            _ = Assert.Single(result.Errors);
            Assert.StartsWith("ScoreFunction", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task AddRule_IncompleteBinaryFormula_Fail()
        {
            // Left and operator present but right missing: a binary formula that is
            // not complete, caught by the delegated FormulaValidator
            var command = ScoringCommand(new FormulaDto
            {
                Operator = ExpressionOperator.GTE,
                Left = new FormulaDto { Variable = Guid.CreateVersion7() },
            });

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.StartsWith("ScoreFunction", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task AddRule_EmptyVariableGuid_Fail()
        {
            // Boundary: Guid.Empty is a syntactically present variable reference
            // that still has to be rejected
            var command = ScoringCommand(new FormulaDto { Variable = Guid.Empty });

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.StartsWith("ScoreFunction", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task AddRule_InvalidOperatorEnum_Fail()
        {
            /*
             * Boundary: 999 is outside every defined ExpressionOperator member. The
             * formula is shape-complete (left, right and operator all present), so
             * only the enum rule can reject it
             */
            var command = ScoringCommand(new FormulaDto
            {
                Operator = (ExpressionOperator)999,
                Left = new FormulaDto { Constant = "1" },
                Right = new FormulaDto { Constant = "2" },
            });

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.StartsWith("ScoreFunction", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task AddRule_AllInvalid_Fail()
        {
            // Boundary: every rule that can fail at once fails independently, so the
            // client learns about all mistakes in a single response
            var command = new AddMetricRuleCommand
            {
                ClinicalMetricId = Guid.Empty,
                CriterionId = Guid.Empty,
            };

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Equal(3, result.Errors.Count);
            Assert.Contains(result.Errors, x => x.PropertyName == "ClinicalMetricId");
            Assert.Contains(result.Errors, x => x.PropertyName == "CriterionId");
            Assert.Contains(result.Errors, x => x.PropertyName?.Length == 0);
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

        // One point for patients at least 65 years old, nothing below - the age band
        // scoring of the real severity metrics
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
    }
}
