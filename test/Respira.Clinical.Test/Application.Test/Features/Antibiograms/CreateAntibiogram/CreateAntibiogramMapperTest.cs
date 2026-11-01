using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Antibiograms.CreateAntibiogram;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.Antibiograms.CreateAntibiogram
{
    public class CreateAntibiogramMapperTest
    {
        private readonly ICreateMapper<CreateAntibiogramCommand, Antibiogram> _mapper =
            new CreateAntibiogramMapper();

        #region Happy path

        // Boundary values of MicCategory: the lowest and highest defined members
        [Theory]
        [InlineData(MicCategory.Susceptible)]
        [InlineData(MicCategory.Resistant)]
        public void ToModel_MapsAntibiogramFields_Success(MicCategory micCategory)
        {
            var pathogenId = Guid.CreateVersion7();
            var command = new CreateAntibiogramCommand
            {
                PathogenId = pathogenId,
                MicCategory = micCategory,
                AntibioticIds = [Guid.CreateVersion7(), Guid.CreateVersion7()],
            };

            var result = _mapper.ToModel(command);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var model = result.Data;

            Assert.Equal(pathogenId, model.PathogenId);
            Assert.Equal(micCategory, model.MicCategory);

            // Base generates the ID so the handler can return it right after saving
            Assert.NotEqual(Guid.Empty, model.Id);

            // The handler fills the join collection through UpdateRelations, so the
            // mapper must leave it empty to avoid inserting duplicate join rows
            Assert.Empty(model.Antibiotics);
        }

        [Fact]
        public void ToModel_WithDependencies_Success()
        {
            // The dependency overload is used by the generic handler pipeline and must
            // produce the same model as the plain overload
            var command = new CreateAntibiogramCommand
            {
                PathogenId = Guid.CreateVersion7(),
                MicCategory = MicCategory.Intermediate,
                AntibioticIds = [Guid.CreateVersion7()],
            };

            var result = _mapper.ToModel(command, new object());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Equal(command.PathogenId, result.Data.PathogenId);
            Assert.Equal(command.MicCategory, result.Data.MicCategory);
            Assert.Empty(result.Data.Antibiotics);
        }

        #endregion

        #region Fail path

        /*
         * Skipped: CreateAntibiogramMapper never fails. The handler's map-failure
         * branch therefore cannot be exercised, because the mapper always returns a
         * successful result for any command.
         */

        #endregion
    }
}
