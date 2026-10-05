using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Pathogens.CreatePathogen;
using Respira.Clinical.Domain.Entities;

namespace Respira.Application.Test.Features.Pathogens.CreatePathogen
{
    public class CreatePathogenMapperTest
    {
        private readonly ICreateMapper<Pathogen, CreatePathogenCommand> _mapper = new CreatePathogenMapper();

        [Fact]
        public void ToModel_Success()
        {
            // Create command 
            var command = new CreatePathogenCommand
            {
                Name = "Test pathogen",
                Description = "Test pathogen description",
                IsAtypical = true
            };

            // Map command to model
            var model = _mapper.ToModel(command);

            Assert.Equal(command.Name, model.Name);
            Assert.Equal(command.Description, model.Description);
            Assert.Equal(command.IsAtypical, model.IsAtypical);
            Assert.NotEqual(Guid.Empty, model.Id);
        }
    }
}
