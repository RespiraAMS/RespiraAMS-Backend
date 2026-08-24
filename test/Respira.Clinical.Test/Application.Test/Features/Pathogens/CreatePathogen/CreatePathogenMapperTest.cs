using Application.Contracts.Mappers;
using Application.Features.Pathogens.CreatePathogen;
using Domain.Models;

namespace Application.Test.Features.Pathogens.CreatePathogen;

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
        };

        // Map command to model
        var model = _mapper.ToModel(command);

        Assert.Equal(command.Name, model.Name);
        Assert.Equal(command.Description, model.Description);
        Assert.NotEqual(Guid.Empty, model.Id);
    }
}
