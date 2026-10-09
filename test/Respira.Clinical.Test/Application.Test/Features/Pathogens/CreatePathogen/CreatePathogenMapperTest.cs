using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Pathogens.CreatePathogen;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.Pathogens.CreatePathogen
{
    public class CreatePathogenMapperTest
    {
        private readonly ICreateMapper<CreatePathogenCommand, Pathogen> _mapper = new CreatePathogenMapper();

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
            var result = _mapper.ToModel(command);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var model = result.Data;


            Assert.Equal(command.Name, model.Name);
            Assert.Equal(command.Description, model.Description);
            Assert.Equal(command.IsAtypical, model.IsAtypical);
            Assert.NotEqual(Guid.Empty, model.Id);
        }
    }
}
