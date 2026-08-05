namespace Application.Features.Antibiotics.UpdateAntibiotic;

public class UpdateAntibioticMapper : IUpdateMapper<Antibiotic, UpdateAntibioticCommand>
{
    public void MapModel(Antibiotic model, UpdateAntibioticCommand command)
    {
        model.Name = command.Name;
        model.AntibioticGroupId = command.AntibioticGroupId;
        model.Category = command.Category;
        model.UpdatedAt = DateTimeOffset.UtcNow;
    }
}