namespace Application.Features.AntibioticGroups.UpdateAntibioticGroup;

public class UpdateAntibioticGroupMapper : IUpdateMapper<AntibioticGroup, UpdateAntibioticGroupCommand>
{
    public void MapModel(AntibioticGroup model, UpdateAntibioticGroupCommand command)
    {
        model.Name = command.Name;
        model.Description = command.Description;
        model.ParentId = command.ParentId;
        model.UpdatedAt = DateTimeOffset.UtcNow;
    }
}