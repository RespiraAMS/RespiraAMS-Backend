namespace Application.Features.Pathogens.DeletePathogen;

public record DeletePathogenCommand(Guid Id) : ICommand
{
    /// <summary>
    /// Pathogen ID
    /// </summary>
    public Guid Id { get; set; } = Id;
}