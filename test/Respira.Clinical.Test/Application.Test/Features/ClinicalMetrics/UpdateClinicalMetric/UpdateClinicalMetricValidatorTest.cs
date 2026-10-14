using Respira.Clinical.Application.Features.ClinicalMetrics.UpdateClinicalMetric;

namespace Respira.Clinical.Application.Test.Features.ClinicalMetrics.UpdateClinicalMetric
{
    public class UpdateClinicalMetricValidatorTest
    {
        private readonly UpdateClinicalMetricValidator _validator = new();

        private static readonly Guid ExistingMetricId = Guid.CreateVersion7();

        #region Valid command

        // The three metrics shipped by the real seed data (seed-data.jsonc): name, code,
        // description, version and publisher are all filled with clinically meaningful values
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
        public async Task UpdateClinicalMetric_SeededMetric_Success(
            string name, string code, string description, string version, string publisher)
        {
            var result = await _validator.ValidateAsync(
                CreateCommand(name, code, description, version, publisher),
                TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpdateClinicalMetric_MaximumId_Success()
        {
            // Upper boundary of the GUID range: every bit set is still a valid id
            var command = CreateCommand() with
            {
                Id = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpdateClinicalMetric_MinimalValues_Success()
        {
            // Boundary: a single character is the shortest value NotEmpty accepts,
            // version "1" is the shortest meaningful version string
            var command = CreateCommand("A", "C1", "d", "1", "P");

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpdateClinicalMetric_ValueWithSurroundingWhitespace_Success()
        {
            // NotEmpty only rejects empty / whitespace-only values, a padded value is
            // still reported as valid
            var command = CreateCommand(" Severity metrics CURB-65 ", " CURB-65 ", " assessment ", " 2002-2003 ", " BTS ");

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid required fields

        [Fact]
        public async Task UpdateClinicalMetric_EmptyId_Fail()
        {
            // Boundary: Guid.Empty is treated as empty by NotEmpty
            var command = CreateCommand() with { Id = Guid.Empty };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Id", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData("")]
        // Whitespace-only value is treated as empty by NotEmpty
        [InlineData("   ")]
        public async Task UpdateClinicalMetric_EmptyName_Fail(string name)
        {
            var command = CreateCommand() with { Name = name };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Name", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateClinicalMetric_EmptyCode_Fail(string code)
        {
            var command = CreateCommand() with { Code = code };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Code", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateClinicalMetric_EmptyDescription_Fail(string description)
        {
            var command = CreateCommand() with { Description = description };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Description", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateClinicalMetric_EmptyVersion_Fail(string version)
        {
            var command = CreateCommand() with { Version = version };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Version", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateClinicalMetric_EmptyPublisher_Fail(string publisher)
        {
            var command = CreateCommand() with { Publisher = publisher };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Publisher", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateClinicalMetric_AllRequiredFieldsEmpty_Fail()
        {
            // Boundary: 0 characters in every string field plus the empty GUID. Each
            // rule fires independently, so the client learns about all mistakes at once
            var command = CreateCommand("", "", "", "", "") with { Id = Guid.Empty };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Equal(6, result.Errors.Count);
            Assert.Contains(result.Errors, x => x.PropertyName == "Id");
            Assert.Contains(result.Errors, x => x.PropertyName == "Name");
            Assert.Contains(result.Errors, x => x.PropertyName == "Code");
            Assert.Contains(result.Errors, x => x.PropertyName == "Description");
            Assert.Contains(result.Errors, x => x.PropertyName == "Version");
            Assert.Contains(result.Errors, x => x.PropertyName == "Publisher");
        }

        #endregion

        private static UpdateClinicalMetricCommand CreateCommand(
            string name = "Severity metrics CURB-65",
            string code = "CURB-65",
            string description = "CURB-65 metrics to assess severity of a patient",
            string version = "2002-2003",
            string publisher = "Hiệp hội Lồng ngực Anh (BTS)")
        {
            return new UpdateClinicalMetricCommand
            {
                Id = ExistingMetricId,
                Name = name,
                Code = code,
                Description = description,
                Version = version,
                Publisher = publisher,
                IsApplied = true,
            };
        }
    }
}
