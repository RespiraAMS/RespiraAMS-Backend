using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.ClinicalMetrics.UpdateClinicalMetric;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.ClinicalMetrics.UpdateClinicalMetric
{
    public class UpdateClinicalMetricMapperTest
    {
        private readonly IUpdateMapper<ClinicalMetric, UpdateClinicalMetricCommand> _mapper =
            new UpdateClinicalMetricMapper();

        #region Happy path

        [Fact]
        public void MapModel_MapsEveryField_Success()
        {
            var model = CreateMetric();
            var command = new UpdateClinicalMetricCommand
            {
                Id = model.Id,
                // Severity metrics CURB-65, published under the name of its guideline
                Name = "Severity metrics CURB-65 (BTS guideline)",
                Description = "CURB-65 metrics to assess severity of a patient, per the BTS guideline",
                Version = "2009",
                Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                IsApplied = true,
            };

            var result = _mapper.MapModel(model, command);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(command.Name, model.Name);
            Assert.Equal(command.Description, model.Description);
            Assert.Equal(command.Version, model.Version);
            Assert.Equal(command.Publisher, model.Publisher);

            // Business rule: the update moves the metric in and out of the in-use set
            Assert.Equal(command.IsApplied, model.IsApplied);

            // The update only rewrites the payload: identity, rules and the soft delete
            // markers of the metric are out of its reach
            Assert.NotEqual(Guid.Empty, model.Id);
            Assert.False(model.IsDeleted);
            Assert.Null(model.DeletedAt);
            Assert.Empty(model.Rules);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MapModel_TogglesIsApplied_Success(bool isApplied)
        {
            // The update is the only way a metric enters or leaves the in-use set
            var model = CreateMetric(isApplied: !isApplied);
            var command = new UpdateClinicalMetricCommand
            {
                Id = model.Id,
                Name = "ICU hospitalization criteria by IDSA/ATS",
                Description = "IDSA/ATS metrics to assess whether a severe patient need ICU or not",
                Version = "2026",
                Publisher = "Hiệp hội Bệnh nhiễm trùng Mỹ (IDSA) và Hiệp hội Lồng ngực Mỹ (ATS)",
                IsApplied = isApplied,
            };

            var result = _mapper.MapModel(model, command);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(isApplied, model.IsApplied);
            Assert.Equal(command.Name, model.Name);
        }

        [Fact]
        public void MapModel_WithDependencies_MapsSameFields_Success()
        {
            // A metric carries no reference resolved by the update, so the overload
            // taking a dependencies argument must produce the same mapping
            var model = CreateMetric();
            var command = new UpdateClinicalMetricCommand
            {
                Id = model.Id,
                Name = "Pneumonia Severity Index (PSI)",
                Description = "PSI metrics to assess severity of a patient with more detail than CURB-65",
                Version = "1997",
                Publisher = "Đội ngũ Nghiên cứu Kết quả Điều trị Viêm phổi (PORT - Pneumonia Outcomes Research Team)",
                IsApplied = true,
            };

            var result = _mapper.MapModel(model, command, new List<MetricRule>());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.Equal(command.Name, model.Name);
            Assert.Equal(command.Description, model.Description);
            Assert.Equal(command.Version, model.Version);
            Assert.Equal(command.Publisher, model.Publisher);
            Assert.True(model.IsApplied);
        }

        [Fact]
        public void MapModel_CodeImmutableAfterUpdating_Success()
        {
            var model = CreateMetric();
            var code = model.Code;

            var command = new UpdateClinicalMetricCommand
            {
                Id = model.Id,
                Name = "Severity metrics CURB-65 (BTS guideline)",
                Description = "CURB-65 metrics to assess severity of a patient, per the BTS guideline",
                Version = "2009",
                Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                IsApplied = true,
            };

            var result = _mapper.MapModel(model, command);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(command.Name, model.Name);
            Assert.Equal(command.Description, model.Description);
            Assert.Equal(command.Version, model.Version);
            Assert.Equal(command.Publisher, model.Publisher);
            Assert.True(model.IsApplied);

            // The code is immutable after creation, so it should remain the same
            Assert.Equal(code, model.Code);
        }

        #endregion

        #region Fail path

        // Skipped: this mapper has no failure mode. It only copies the command payload
        // onto the loaded metric, a shape the domain model always accepts.

        #endregion

        private static ClinicalMetric CreateMetric(bool isApplied = true)
        {
            return new ClinicalMetric
            {
                Name = "Severity metrics CURB-65",
                Code = "CURB-65",
                Description = "CURB-65 metrics to assess severity of a patient",
                Version = "2002-2003",
                Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                IsApplied = isApplied,
                Rules = [],
            };
        }
    }
}
