namespace Application.Features.Antibiograms.UpdateAntibiogram;

public class UpdateAntibiogramMapper : IUpdateMapper<Antibiogram, UpdateAntibiogramCommand>
{
    public void MapModel(Antibiogram model, UpdateAntibiogramCommand command)
    {
        // Not allow to change pathogen on antibiogram
        model.MicLevel = command.MicLevel;
        model.UpdatedAt = DateTimeOffset.UtcNow;
    }
}