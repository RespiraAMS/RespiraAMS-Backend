namespace Application.Features.Diseases.UpdateDisease;

public class UpdateDiseaseCommand : ICommand
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required int IcuScoreThreshold { get; set; }
}