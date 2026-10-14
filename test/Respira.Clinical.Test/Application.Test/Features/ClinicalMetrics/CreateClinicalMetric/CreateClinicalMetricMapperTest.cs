using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.ClinicalMetrics.CreateClinicalMetric;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.ClinicalMetrics.CreateClinicalMetric
{
    public class CreateClinicalMetricMapperTest
    {
        private readonly ICreateMapper<CreateClinicalMetricCommand, ClinicalMetric> _mapper =
            new CreateClinicalMetricMapper();

        #region Map command to model

        // The three metrics shipped by the real seed data (seed-data.jsonc)
        [Theory]
        [InlineData(
            "Severity metrics CURB-65",
            "CURB-65",
            "CURB-65 metrics to assess severity of a patient",
            "2002-2003",
            "Hiệp hội Lồng ngực Anh (BTS)")]
        [InlineData(
            "Pneumonia Severity Index (PSI)",
            "PSI",
            "PSI metrics to assess severity of a patient with more detail than CURB-65",
            "1997",
            "Đội ngũ Nghiên cứu Kết quả Điều trị Viêm phổi (PORT - Pneumonia Outcomes Research Team)")]
        [InlineData(
            "ICU hospitalization criteria by IDSA/ATS",
            "IDSA/ATS",
            "IDSA/ATS metrics to assess whether a severe patient need ICU or not",
            "2026",
            "Hiệp hội Bệnh nhiễm trùng Mỹ (IDSA) và Hiệp hội Lồng ngực Mỹ (ATS)")]
        public void ToModel_MapsEveryField_Success(
            string name, string code, string description, string version, string publisher)
        {
            var command = new CreateClinicalMetricCommand
            {
                Name = name,
                Code = code,
                Description = description,
                Version = version,
                Publisher = publisher,
            };

            var result = _mapper.ToModel(command);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Equal(command.Name, result.Data.Name);
            Assert.Equal(command.Code, result.Data.Code);
            Assert.Equal(command.Description, result.Data.Description);
            Assert.Equal(command.Version, result.Data.Version);
            Assert.Equal(command.Publisher, result.Data.Publisher);
            Assert.NotEqual(Guid.Empty, result.Data.Id);
            Assert.False(result.Data.IsDeleted);
            Assert.Null(result.Data.DeletedAt);

            // Business rule: a newly created metric is always in use
            Assert.True(result.Data.IsApplied);

            // Business rule: the feature creates the metric without any rule, so it
            // belongs to neither the scoring nor the major/minor system yet
            Assert.Empty(result.Data.Rules);
            Assert.False(result.Data.IsScoringMetric);
            Assert.False(result.Data.IsMajorMinorMetric);
        }

        [Fact]
        public void ToModel_WithDependencies_MapsSameMetric_Success()
        {
            /*
             * The metric creation never resolves any dependency (a fresh metric has no
             * rule yet), so the overload taking a dependencies argument must produce the
             * same model as the dependency-less one
             */
            var command = new CreateClinicalMetricCommand
            {
                Name = "Pneumonia Severity Index (PSI)",
                Code = "PSI",
                Description = "PSI metrics to assess severity of a patient with more detail than CURB-65",
                Version = "1997",
                Publisher = "Đội ngũ Nghiên cứu Kết quả Điều trị Viêm phổi (PORT - Pneumonia Outcomes Research Team)",
            };

            var result = _mapper.ToModel(command, new List<MetricRule>());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Equal(command.Name, result.Data.Name);
            Assert.Equal(command.Code, result.Data.Code);
            Assert.Equal(command.Description, result.Data.Description);
            Assert.Equal(command.Version, result.Data.Version);
            Assert.Equal(command.Publisher, result.Data.Publisher);
            Assert.NotEqual(Guid.Empty, result.Data.Id);
            Assert.True(result.Data.IsApplied);
            Assert.Empty(result.Data.Rules);
        }

        #endregion

        #region Fail path

        // Skipped: this mapper has no failure mode. It only builds a metric without
        // rules, and the ClinicalMetric constructor only rejects a rule collection
        // mixing ScoringRule with MajorMinorRule, a shape an empty collection never
        // has. The dependency-less and the dependencies overload are both covered above.

        #endregion
    }
}
