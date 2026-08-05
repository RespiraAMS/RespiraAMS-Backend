namespace Application.Features.Causes.UpdateCause;

public class UpdateCauseMapper : IUpdateMapper<Cause, UpdateCauseCommand>
{
    public void MapModel(Cause model, UpdateCauseCommand command)
    {
        model.Severity = command.Severity;
        model.TreatmentSite = command.TreatmentSite;
        model.UpdatedAt = DateTimeOffset.UtcNow;
    }
}