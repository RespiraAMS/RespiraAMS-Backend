namespace Application.Features.Antibiotics.DeleteDosage;

public class DeleteDosageCommand : ICommand
{
    /// <summary>
    /// Dosage ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Antibiotic ID
    /// </summary>
    public required Guid AntibioticId { get; set; }
}
