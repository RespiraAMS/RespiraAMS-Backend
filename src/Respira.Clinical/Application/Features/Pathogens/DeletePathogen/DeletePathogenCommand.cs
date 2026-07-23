namespace Application.Features.Pathogens.DeletePathogen;

public class DeletePathogenCommand(Guid id) : ICommand
{
    public Guid Id { get; set; } = id;
}