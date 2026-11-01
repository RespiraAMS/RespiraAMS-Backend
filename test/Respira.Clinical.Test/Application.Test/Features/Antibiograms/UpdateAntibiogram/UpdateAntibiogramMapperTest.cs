using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Antibiograms.UpdateAntibiogram;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.Antibiograms.UpdateAntibiogram
{
    public class UpdateAntibiogramMapperTest
    {
        private readonly IUpdateMapper<Antibiogram, UpdateAntibiogramCommand> _mapper =
            new UpdateAntibiogramMapper();

        #region Happy path

        // Boundary values of MicCategory: the lowest and highest defined members
        [Theory]
        [InlineData(MicCategory.Susceptible)]
        [InlineData(MicCategory.Resistant)]
        public void MapModel_MapsAntibiogramFields_Success(MicCategory micCategory)
        {
            var originalUpdatedAt =
                new DateTimeOffset(2026, 1, 15, 8, 30, 0, TimeSpan.Zero);
            var model = new Antibiogram
            {
                PathogenId = Guid.CreateVersion7(),
                MicCategory = MicCategory.Intermediate,
                UpdatedAt = originalUpdatedAt,
            };
            var modelId = model.Id;
            var newPathogenId = Guid.CreateVersion7();

            var command = new UpdateAntibiogramCommand
            {
                Id = modelId,
                PathogenId = newPathogenId,
                MicCategory = micCategory,
                AntibioticIds = [Guid.CreateVersion7(), Guid.CreateVersion7()],
            };

            var result = _mapper.MapModel(model, command);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            // Business rule: the susceptibility result is overwritten with the
            // pathogen and MIC category from the command
            Assert.Equal(newPathogenId, model.PathogenId);
            Assert.Equal(micCategory, model.MicCategory);

            // Business rule: the update timestamp is refreshed on every update
            Assert.True(model.UpdatedAt > originalUpdatedAt);

            // Identity and joins are owned by the handler, not the mapper
            Assert.Equal(modelId, model.Id);
            Assert.Empty(model.Antibiotics);
        }

        [Fact]
        public void MapModel_DoesNotTouchAntibioticCollection_Success()
        {
            /*
             * UpdateRelations owns the join collection: it removes unlisted antibiotics
             * and attaches stubs for the new ones. The mapper must leave the collection
             * exactly as it found it so the handler can compute the difference
             */
            var existingAntibiotic = new Antibiotic
            {
                Name = "Ceftriaxone",
                AntibioticGroupId = Guid.CreateVersion7(),
                Classification = AwareClassification.Watch,
            };
            var model = new Antibiogram
            {
                PathogenId = Guid.CreateVersion7(),
                MicCategory = MicCategory.Susceptible,
                Antibiotics = [existingAntibiotic],
            };
            var originalUpdatedAt = model.UpdatedAt;

            var command = new UpdateAntibiogramCommand
            {
                Id = model.Id,
                PathogenId = Guid.CreateVersion7(),
                MicCategory = MicCategory.Resistant,
                AntibioticIds = [Guid.CreateVersion7()],
            };

            var result = _mapper.MapModel(model, command);

            Assert.True(result.IsSuccess());
            Assert.True(model.UpdatedAt >= originalUpdatedAt);
            var kept = Assert.Single(model.Antibiotics);
            Assert.Equal(existingAntibiotic.Id, kept.Id);
            Assert.Equal("Ceftriaxone", kept.Name);
        }

        [Fact]
        public void MapModel_WithDependencies_Success()
        {
            // The dependency overload is used by the generic handler pipeline and must
            // produce the same model changes as the plain overload
            var model = new Antibiogram
            {
                PathogenId = Guid.CreateVersion7(),
                MicCategory = MicCategory.Susceptible,
            };
            var command = new UpdateAntibiogramCommand
            {
                Id = model.Id,
                PathogenId = Guid.CreateVersion7(),
                MicCategory = MicCategory.Intermediate,
                AntibioticIds = [Guid.CreateVersion7()],
            };

            var result = _mapper.MapModel(model, command, new object());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.Equal(command.PathogenId, model.PathogenId);
            Assert.Equal(command.MicCategory, model.MicCategory);
            Assert.Empty(model.Antibiotics);
        }

        #endregion

        #region Fail path

        /*
         * Skipped: UpdateAntibiogramMapper never fails. The handler's map-failure
         * branch is therefore unreachable for any command.
         */

        #endregion
    }
}
