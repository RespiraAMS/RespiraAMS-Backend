namespace Application.Features.Antibiotics.DeleteDosage;

public class DeleteDosageCommand : ICommand
{
    public required Guid Id { get; set; }
}