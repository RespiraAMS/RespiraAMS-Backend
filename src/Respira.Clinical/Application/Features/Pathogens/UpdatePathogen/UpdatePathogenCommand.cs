namespace Application.Features.Pathogens.UpdatePathogen;

public class UpdatePathogenCommand : ICommand
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}