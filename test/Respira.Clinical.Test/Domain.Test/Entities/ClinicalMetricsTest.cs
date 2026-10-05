using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Models;
using Xunit;

namespace Respira.Clinical.Domain.Test.Entities
{
    public class ClinicalMetricsTest
    {
        private static ScoringRule NewScoringRule() => new()
        {
            ClinicalMetricsId = Guid.CreateVersion7(),
            CriterionId = Guid.CreateVersion7(),
            ScoreFunction = new NumericConstantFormula(1),
        };

        private static MajorMinorRule NewMajorMinorRule() => new()
        {
            ClinicalMetricsId = Guid.CreateVersion7(),
            CriterionId = Guid.CreateVersion7(),
            IsMajor = true,
        };

        [Fact]
        public void EmptyRules_BelongsToNeitherSystem()
        {
            var metrics = new ClinicalMetrics("Metrics", "CODE", "Description", []);

            Assert.False(metrics.IsScoringMetric);
            Assert.False(metrics.IsMajorMinorMetric);
        }

        [Fact]
        public void AllScoringRules_IsScoringMetricOnly()
        {
            var metrics = new ClinicalMetrics("Metrics", "CODE", "Description", [NewScoringRule(), NewScoringRule()]);

            Assert.True(metrics.IsScoringMetric);
            Assert.False(metrics.IsMajorMinorMetric);
        }

        [Fact]
        public void AllMajorMinorRules_IsMajorMinorMetricOnly()
        {
            var metrics = new ClinicalMetrics("Metrics", "CODE", "Description", [NewMajorMinorRule(), NewMajorMinorRule()]);

            Assert.True(metrics.IsMajorMinorMetric);
            Assert.False(metrics.IsScoringMetric);
        }

        [Fact]
        public void MixedRuleTypes_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new ClinicalMetrics("Metrics", "CODE", "Description", [NewScoringRule(), NewMajorMinorRule()]));
        }
    }
}
