using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Pathogens.UpdatePathogen;
using Respira.Clinical.Domain.Entities;

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

            _mapper.MapModel(model, command);

            Assert.Equal(command.Name, model.Name);
            Assert.Equal(command.Description, model.Description);
            Assert.NotEqual(updatedAtBeforeMapping, model.UpdatedAt);
        }

        #endregion
    }
}
