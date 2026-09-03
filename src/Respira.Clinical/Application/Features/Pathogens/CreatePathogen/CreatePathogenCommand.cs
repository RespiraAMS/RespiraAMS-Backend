namespace Application.Features.Pathogens.CreatePathogen;

public record CreatePathogenCommand : ICommand
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

public record CreatePathogenResult(Guid Id)
{
    /// <summary>
    /// Pathogen ID
    /// </summary>
    public Guid Id { get; set; } = Id;
}