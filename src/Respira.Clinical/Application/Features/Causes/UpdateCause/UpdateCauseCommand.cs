using Domain.Enums;
using Severity = Domain.Enums.Severity;

namespace Application.Features.Causes.UpdateCause;

public class UpdateCauseCommand : ICommand
{
    public required Guid Id { get; set; }
    public required Severity Severity { get; set; }
    public required TreatmentSite TreatmentSite { get; set; }
}