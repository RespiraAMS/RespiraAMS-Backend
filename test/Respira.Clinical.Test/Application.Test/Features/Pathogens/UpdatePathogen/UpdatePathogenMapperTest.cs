using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Pathogens.UpdatePathogen;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.Pathogens.UpdatePathogen
{
    public class UpdatePathogenMapperTest
    {
        private readonly IUpdateMapper<Pathogen, UpdatePathogenCommand> _mapper = new UpdatePathogenMapper();

        #region Happy path

        [Fact]
        public void MapModel_Success()
        {
            var model = new Pathogen
            {
                Name = "Klebsiella pneumoniae",
                Description = "Gram-negative bacillus",
                IsAtypical = true,
            };
            var updatedAtBeforeMapping = model.UpdatedAt;

            var command = new UpdatePathogenCommand
            {
                Id = model.Id,
                Name = "Klebsiella variicola",
                Description = "Closely related species found in hospital settings",
                IsAtypical = false,
            };

            var result = _mapper.MapModel(model, command);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(command.Name, model.Name);
            Assert.Equal(command.Description, model.Description);
            Assert.NotEqual(updatedAtBeforeMapping, model.UpdatedAt);
        }

        #endregion
    }
}
