using Respira.Clinical.Application.Features.ClinicalMetrics.CreateClinicalMetric;

namespace Respira.Application.Test.Features.ClinicalMetrics.CreateClinicalMetric
{
    public class CreateClinicalMetricValidatorTest
    {
        private readonly CreateClinicalMetricValidator _validator = new();

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
        public async Task CreateClinicalMetric_SeededMetric_Success(
            string name, string code, string description, string version, string publisher)
        {
            var result = await _validator.ValidateAsync(
                CreateCommand(name, code, description, version, publisher),
                TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task CreateClinicalMetric_MinimalValues_Success()
        {
            // Boundary: a single character is the shortest value NotEmpty accepts,
            // version "1" is the shortest meaningful version string
            var command = CreateCommand("A", "C1", "d", "1", "P");

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task CreateClinicalMetric_ValueWithSurroundingWhitespace_Success()
        {
            // NotEmpty only rejects empty / whitespace-only values, a padded value is
            // still reported as valid
            var command = CreateCommand(" Severity metrics CURB-65 ", " CURB-65 ", " assessment ", " 2002-2003 ", " BTS ");

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid required fields

        [Theory]
        [InlineData("")]
        // Whitespace-only value is treated as empty by NotEmpty
        [InlineData("   ")]
        public async Task CreateClinicalMetric_EmptyName_Fail(string name)
        {
            var command = CreateCommand();
            command.Name = name;

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Name", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateClinicalMetric_EmptyCode_Fail(string code)
        {
            var command = CreateCommand();
            command.Code = code;

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Code", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateClinicalMetric_EmptyDescription_Fail(string description)
        {
            var command = CreateCommand();
            command.Description = description;

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Description", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateClinicalMetric_EmptyVersion_Fail(string version)
        {
            var command = CreateCommand();
            command.Version = version;

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Version", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateClinicalMetric_EmptyPublisher_Fail(string publisher)
        {
            var command = CreateCommand();
            command.Publisher = publisher;

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Publisher", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task CreateClinicalMetric_AllRequiredFieldsEmpty_Fail()
        {
            // Boundary: 0 characters in every required field. Each rule fires
            // independently, so the client learns about all mistakes at once
            var command = CreateCommand("", "", "", "", "");

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Equal(5, result.Errors.Count);
            Assert.Contains(result.Errors, x => x.PropertyName == "Name");
            Assert.Contains(result.Errors, x => x.PropertyName == "Code");
            Assert.Contains(result.Errors, x => x.PropertyName == "Description");
            Assert.Contains(result.Errors, x => x.PropertyName == "Version");
            Assert.Contains(result.Errors, x => x.PropertyName == "Publisher");
        }

        #endregion

        private static CreateClinicalMetricCommand CreateCommand(
            string name = "Severity metrics CURB-65",
            string code = "CURB-65",
            string description = "CURB-65 metrics to assess severity of a patient",
            string version = "2002-2003",
            string publisher = "Hiệp hội Lồng ngực Anh (BTS)")
        {
            return new CreateClinicalMetricCommand
            {
                Name = name,
                Code = code,
                Description = description,
                Version = version,
                Publisher = publisher,
            };
        }
    }
}
