namespace Application.Features.Pathogens.CreatePathogen;

public class CreatePathogenMapper : ICreateMapper<Pathogen, CreatePathogenCommand>
{
    public Pathogen ToModel(CreatePathogenCommand command)
    {
        return new Pathogen()
        {
            Name = command.Name,
            Description = command.Description
        };
    }
}