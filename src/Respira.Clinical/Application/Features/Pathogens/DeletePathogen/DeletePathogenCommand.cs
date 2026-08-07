namespace Application.Features.Pathogens.DeletePathogen;

public class DeletePathogenCommand(Guid id) : ICommand
{
    /// <summary>
    /// Pathogen ID
    /// </summary>
    public Guid Id { get; set; } = id;
}