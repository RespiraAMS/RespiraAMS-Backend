namespace Application.Features.Antibiotics.DeleteAntibiotic;

public class DeleteAntibioticCommand : ICommand
{
    /// <summary>
    /// Antibiotic ID
    /// </summary>
    public required Guid Id { get; set; }
}