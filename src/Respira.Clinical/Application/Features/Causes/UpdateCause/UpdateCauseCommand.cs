using Domain.Enums;
using Severity = Domain.Enums.Severity;

namespace Application.Features.Causes.UpdateCause;

public record UpdateCauseCommand : ICommand
{
    /// <summary>
    /// Disease's cause ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Severity caused by this pathogen
    /// </summary>
    public required Severity Severity { get; set; }

    /// <summary>
    /// Treatment site assigned to patient when catching the disease with this pathogen
    /// </summary>
    public required TreatmentSite TreatmentSite { get; set; }
}