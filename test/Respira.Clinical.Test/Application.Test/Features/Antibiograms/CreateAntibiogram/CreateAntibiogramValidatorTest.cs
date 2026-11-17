using Respira.Clinical.Application.Features.Antibiograms.CreateAntibiogram;
using Respira.Clinical.Domain.Enums;

namespace Respira.Clinical.Application.Test.Features.Antibiograms.CreateAntibiogram
{
    public class CreateAntibiogramValidatorTest
    {
        private readonly CreateAntibiogramValidator _validator = new();

        #region Valid command

        [Fact]
        public async Task CreateAntibiogram_MinimalValues_Success()
        {
            // Boundary: exactly 1 antibiotic, the minimum accepted by NotEmpty
            var command = CreateCommand(antibioticIds: [Guid.CreateVersion7()]);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task CreateAntibiogram_MultipleAntibiotics_Success()
        {
            var command = CreateCommand(antibioticIds:
            [
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
            ]);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Every defined MicCategory member is valid
        [Theory]
        [InlineData(MicCategory.Susceptible)]
        [InlineData(MicCategory.Intermediate)]
        [InlineData(MicCategory.Resistant)]
        public async Task CreateAntibiogram_MicCategoryBoundaries_Success(MicCategory micCategory)
        {
            var command = CreateCommand() with { MicCategory = micCategory };

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        [Fact]
        public async Task CreateAntibiogram_EmptyPathogenId_Fail()
        {
            // Boundary: Guid.Empty is the only UUID value rejected by NotEmpty
            var command = CreateCommand() with { PathogenId = Guid.Empty };

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("PathogenId", result.Errors[0].PropertyName);
        }

        // MicCategory members are 0, 1 and 2, so -1 and 3 are the adjacent invalid values
        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        public async Task CreateAntibiogram_InvalidMicCategory_Fail(int micCategory)
        {
            var command = CreateCommand() with { MicCategory = (MicCategory)micCategory };

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("MicCategory", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task CreateAntibiogram_EmptyAntibioticList_Fail()
        {
            // Boundary: 0 antibiotics, one below the minimum of 1
            var command = CreateCommand(antibioticIds: []);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("AntibioticIds", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task CreateAntibiogram_DuplicateAntibioticIds_Fail()
        {
            // The same antibiotic may not be listed twice for one pathogen
            var antibioticId = Guid.CreateVersion7();
            var command = CreateCommand(antibioticIds: [antibioticId, antibioticId]);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("AntibioticIds", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task CreateAntibiogram_EmptyAntibioticId_Fail()
        {
            var command = CreateCommand(antibioticIds: [Guid.CreateVersion7(), Guid.Empty]);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("AntibioticIds[1]", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task CreateAntibiogram_MultipleEmptyAntibioticIds_Fail()
        {
            /*
             * RuleForEach reports one error per offending entry. Repeating Guid.Empty
             * also trips the duplicate rule, so all three violations must be reported
             */
            var command = CreateCommand(antibioticIds:
            [
                Guid.Empty,
                Guid.CreateVersion7(),
                Guid.Empty,
            ]);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Equal(3, result.Errors.Count);
            Assert.Contains(result.Errors, x => x.PropertyName == "AntibioticIds");
            Assert.Contains(result.Errors, x => x.PropertyName == "AntibioticIds[0]");
            Assert.Contains(result.Errors, x => x.PropertyName == "AntibioticIds[2]");
        }

        [Fact]
        public async Task CreateAntibiogram_AllFieldsInvalid_Fail()
        {
            var command = new CreateAntibiogramCommand
            {
                PathogenId = Guid.Empty,
                MicCategory = (MicCategory)(-1),
                AntibioticIds = [],
            };

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Equal(3, result.Errors.Count);
            Assert.Contains(result.Errors, x => x.PropertyName == "PathogenId");
            Assert.Contains(result.Errors, x => x.PropertyName == "MicCategory");
            Assert.Contains(result.Errors, x => x.PropertyName == "AntibioticIds");
        }

        #endregion

        private static CreateAntibiogramCommand CreateCommand(List<Guid>? antibioticIds = null)
        {
            return new CreateAntibiogramCommand
            {
                PathogenId = Guid.CreateVersion7(),
                MicCategory = MicCategory.Susceptible,
                AntibioticIds = antibioticIds ?? [Guid.CreateVersion7()],
            };
        }
    }
}
