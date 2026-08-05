namespace Application.Features.Antibiograms.DeleteAntibiogram;

public class DeleteAntibiogramCommand : ICommand
{
    public required Guid Id { get; set; }
}