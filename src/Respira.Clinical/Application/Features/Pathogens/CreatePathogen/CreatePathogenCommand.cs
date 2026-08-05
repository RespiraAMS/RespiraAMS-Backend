namespace Application.Features.Pathogens.CreatePathogen;

public class CreatePathogenCommand : ICommand
{
    public required string Name { get; set; }
    public required string Description { get; set; }
}

public class CreatePathogenResult(Guid id)
{
    public Guid Id { get; set; } = id;
}