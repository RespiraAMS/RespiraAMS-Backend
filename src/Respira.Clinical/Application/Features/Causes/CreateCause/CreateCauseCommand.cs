using Domain.Enums;
using Severity = Domain.Enums.Severity;

namespace Application.Features.Causes.CreateCause;

public record CreateCauseCommand : ICommand
{
    /// <summary>
    /// Disease ID
    /// </summary>
    public required Guid DiseaseId { get; set; }

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
}

public record CreateCauseResult(Guid Id)
{
    /// <summary>
    /// Disease's cause ID
    /// </summary>
    public Guid Id { get; set; } = Id;
}