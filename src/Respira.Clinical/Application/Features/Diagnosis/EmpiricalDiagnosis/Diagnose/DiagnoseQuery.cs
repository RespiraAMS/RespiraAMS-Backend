using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Diagnosis.EmpiricalDiagnosis.Diagnose
{
    public record Observation(Guid VariableId, string Value);
    public record DiagnoseQuery : IQuery
    {
        public required IEnumerable<Observation> Observations { get; set; }
    }

    public record PathogenResult(Guid Id, string Name);
    public record ScoredPathogenResult(Guid Id, string Name, decimal Score);

    public record DiagnoseResult
    {
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
        public required IEnumerable<PathogenResult> WorthSuspected { get; set; }
        public required IEnumerable<ScoredPathogenResult> HeavySuspected { get; set; }
    }
}
