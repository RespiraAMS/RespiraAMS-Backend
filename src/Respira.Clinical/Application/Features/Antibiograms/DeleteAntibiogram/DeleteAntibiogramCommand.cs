namespace Application.Features.Antibiograms.DeleteAntibiogram;

public record DeleteAntibiogramCommand(Guid Id) : ICommand
{
    /// <summary>
    /// Antibiogram ID
    /// </summary>
    public Guid Id { get; set; } = Id;
}