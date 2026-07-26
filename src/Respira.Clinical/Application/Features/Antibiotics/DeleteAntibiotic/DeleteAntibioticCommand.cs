namespace Application.Features.Antibiotics.DeleteAntibiotic;

public class DeleteAntibioticCommand : ICommand
{
    public required Guid Id { get; set; }
}