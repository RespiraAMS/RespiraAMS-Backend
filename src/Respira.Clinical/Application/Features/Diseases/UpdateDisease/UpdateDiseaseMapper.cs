namespace Application.Features.Diseases.UpdateDisease;

public class UpdateDiseaseMapper : IUpdateMapper<Disease, UpdateDiseaseCommand>
{
    public void MapModel(Disease model, UpdateDiseaseCommand command)
    {
        model.Name = command.Name;
        model.Description = command.Description;
        model.IcuScoreThreshold = command.IcuScoreThreshold;
        model.UpdatedAt = DateTimeOffset.UtcNow;
    }
}