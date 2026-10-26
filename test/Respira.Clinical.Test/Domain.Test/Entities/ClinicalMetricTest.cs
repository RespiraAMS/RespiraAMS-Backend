using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Models;
using Xunit;

namespace Respira.Clinical.Domain.Test.Entities
{
    public class ClinicalMetricTest
    {
        private static ScoringRule NewScoringRule() => new()
        {
            ClinicalMetricId = Guid.CreateVersion7(),
            CriterionId = Guid.CreateVersion7(),
            ScoreFunction = new NumericConstantFormula(1),
        };

        private static MajorMinorRule NewMajorMinorRule() => new()
        {
            ClinicalMetricId = Guid.CreateVersion7(),
            CriterionId = Guid.CreateVersion7(),
            IsMajor = true,
        };

        [Fact]
        public void EmptyRules_BelongsToNeitherSystem()
        {
            var metrics = new ClinicalMetric
            {
                Name = "Metric",
                Code = "CODE",
                Description = "Description",
                Version = "",
                Publisher = "",
                IsApplied = true,
                Rules = [],
            };

            Assert.False(metrics.IsScoringMetric);
            Assert.False(metrics.IsMajorMinorMetric);
        }

        [Fact]
        public void AllScoringRules_IsScoringMetricOnly()
        {
            var metrics = new ClinicalMetric
            {
                Name = "Metric",
                Code = "CODE",
                Description = "Description",
                Version = "",
                Publisher = "",
                IsApplied = true,
                Rules = [NewScoringRule(), NewScoringRule()],
            };

            Assert.True(metrics.IsScoringMetric);
            Assert.False(metrics.IsMajorMinorMetric);
        }

        [Fact]
        public void AllMajorMinorRules_IsMajorMinorMetricOnly()
        {
            var metrics = new ClinicalMetric
            {
                Name = "Metric",
                Code = "CODE",
                Description = "Description",
                Version = "",
                Publisher = "",
                IsApplied = true,
                Rules = [NewMajorMinorRule(), NewMajorMinorRule()],
            };

            Assert.True(metrics.IsMajorMinorMetric);
            Assert.False(metrics.IsScoringMetric);
        }

        [Fact]
        public void MixedRuleTypes_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ClinicalMetric
            {
                Name = "Metric",
                Code = "CODE",
                Description = "Description",
                Version = "",
                Publisher = "",
                IsApplied = true,
                Rules = [NewScoringRule(), NewMajorMinorRule()],
            });
        }
    }
}
