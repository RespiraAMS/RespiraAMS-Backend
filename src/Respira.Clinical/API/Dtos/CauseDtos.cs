using Application.Features.Causes.CreateCause;
using Application.Features.Causes.UpdateCause;
using Domain.Enums;

namespace Respira.Clinical.API.Dtos;

public class CreateCauseRequestDto
{
    public required Guid PathogenId { get; set; }
    public required Severity Severity { get; set; }
    public required TreatmentSite TreatmentSite { get; set; }

    public CreateCauseCommand ToCommand(Guid diseaseId)
    {
        return new CreateCauseCommand
        {
            DiseaseId = diseaseId,
            PathogenId = PathogenId,
            Severity = Severity,
            TreatmentSite = TreatmentSite
        };
    }
}

public class UpdateCauseRequestDto
{
    public required Severity Severity { get; set; }
    public required TreatmentSite TreatmentSite { get; set; }

    public UpdateCauseCommand ToCommand(Guid id)
    {
        return new UpdateCauseCommand
        {
            Id = id,
            Severity = Severity,
            TreatmentSite = TreatmentSite
        };
    }
}