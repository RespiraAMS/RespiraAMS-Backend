namespace Application.Features.Causes.CreateCause;

public class CreateCauseMapper : ICreateMapper<Cause, CreateCauseCommand>
{
    public Cause ToModel(CreateCauseCommand command)
    {
        return new Cause
        {
            DiseaseId = command.DiseaseId,
            PathogenId = command.PathogenId,
            Severity = command.Severity,
            TreatmentSite = command.TreatmentSite,
        };
    }
}