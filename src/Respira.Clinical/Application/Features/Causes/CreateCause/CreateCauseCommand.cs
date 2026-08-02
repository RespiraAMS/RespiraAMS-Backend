using Domain.Enums;
using Severity = Domain.Enums.Severity;

namespace Application.Features.Causes.CreateCause;

public class CreateCauseCommand : ICommand
{
    public required Guid DiseaseId { get; set; }
    public required Guid PathogenId { get; set; }
    public required Severity Severity { get; set; }
    public required TreatmentSite TreatmentSite { get; set; }
}

public class CreateCauseResult(Guid id)
{
    public Guid Id { get; set; } = id;
}