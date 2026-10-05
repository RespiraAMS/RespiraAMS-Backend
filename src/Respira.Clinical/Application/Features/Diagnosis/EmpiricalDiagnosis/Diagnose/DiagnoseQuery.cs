using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Diagnosis.EmpiricalDiagnosis.Diagnose
{
    public record Observation(Guid VariableId, string Value);
    public record DiagnoseQuery : IQuery
    {
        public required IEnumerable<Observation> Observations { get; set; }
        public required IEnumerable<Guid> Allergies { get; set; }
    }

    public record ClinicalVariableResult(Guid Id, string Name, string Code);
    public record PathogenResult(Guid Id, string Name);
    public record AntibioticResult(Guid Id, string Name);

    public record DiagnoseResult
    {
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
        public required IEnumerable<PathogenResult> WorthSuspected { get; set; }
        public required IEnumerable<PathogenResult> HeavySuspected { get; set; }
        public required IEnumerable<string> Evidences { get; set; }
        public required IEnumerable<ClinicalVariableResult> MissingVariables { get; set; }
        public required IEnumerable<AntibioticResult> Allergies { get; set; }
        public required bool IsPatientPregnantOrInLactationPhase { get; set; }
    }
}
