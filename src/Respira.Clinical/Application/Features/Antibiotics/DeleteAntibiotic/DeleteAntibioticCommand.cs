namespace Application.Features.Antibiotics.DeleteAntibiotic;

public record DeleteAntibioticCommand(Guid Id) : ICommand
{
    /// <summary>
    /// Antibiotic ID
    /// </summary>
    public Guid Id { get; set; } = Id;
}