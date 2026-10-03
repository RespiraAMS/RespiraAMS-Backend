using Respira.Clinical.Domain.Models;
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
        public required SeverityDiagnosis SeverityDiagnosis { get; set; }
        public required IEnumerable<PathogenResult> WorthSuspected { get; set; }
        public required IEnumerable<ScoredPathogenResult> HeavySuspected { get; set; }
        public required IEnumerable<string> Evidences { get; set; }
    }
}
