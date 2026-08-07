namespace Application.Features.Pathogens.CreatePathogen;

public class CreatePathogenCommand : ICommand
{
    /// <summary>
    /// Pathogen name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Pathogen description
    /// </summary>
    public required string Description { get; set; }
}

public class CreatePathogenResult(Guid id)
{
    /// <summary>
    /// Pathogen ID
    /// </summary>
    public Guid Id { get; set; } = id;
}