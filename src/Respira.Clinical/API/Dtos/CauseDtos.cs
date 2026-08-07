using Application.Features.Causes.CreateCause;
using Application.Features.Causes.UpdateCause;
using Domain.Enums;

namespace Respira.Clinical.API.Dtos;

public class CreateCauseRequestDto
{
    /// <summary>
    /// Pathogen ID
    /// </summary>
    public required Guid PathogenId { get; set; }

    /// <summary>
    /// Severity caused by this pathogen
    /// </summary>
    public required Severity Severity { get; set; }

    /// <summary>
    /// Treatment site assigned to patient when catching the disease with this pathogen
    /// </summary>
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
    /// <summary>
    /// Severity caused by this pathogen
    /// </summary>
    public required Severity Severity { get; set; }

    /// <summary>
    /// Treatment site assigned to patient when catching the disease with this pathogen
    /// </summary>
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