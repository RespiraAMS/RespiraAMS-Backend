namespace Application.Features.Antibiograms.DeleteAntibiogram;

public class DeleteAntibiogramCommand : ICommand
{
    /// <summary>
    /// Antibiogram ID
    /// </summary>
    public required Guid Id { get; set; }
}