namespace Application.Features.Pathogens.UpdatePathogen;

public class UpdatePathogenCommand : ICommand
{
    /// <summary>
    /// Pathogen ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Pathogen name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Pathogen description
    /// </summary>
    public required string Description { get; set; }
}