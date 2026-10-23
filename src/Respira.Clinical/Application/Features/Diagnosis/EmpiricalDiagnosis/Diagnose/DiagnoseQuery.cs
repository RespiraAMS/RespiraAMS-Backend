using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
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
    public record MetricsSeverityDiagnosisResult
    {
        public string Code { get; set; }
        public Severity Severity { get; set; }
        public TreatmentSite TreatmentSite { get; set; }
        public int? Score { get; set; }
        public int? MajorMatched { get; set; }
        public int? MinorMatched { get; set; }

        public MetricsSeverityDiagnosisResult(string code, Severity severity, TreatmentSite treatmentSite, int score)
        {
            Code = code;
            Severity = severity;
            TreatmentSite = treatmentSite;
            Score = score;
        }

        public MetricsSeverityDiagnosisResult(string code, Severity severity, TreatmentSite treatmentSite, int majorMatched, int minorMatched)
        {
            Code = code;
            Severity = severity;
            TreatmentSite = treatmentSite;
            MajorMatched = majorMatched;
            MinorMatched = minorMatched;
        }
    }

    public record DiagnoseResult
    {
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
        public required IEnumerable<MetricsSeverityDiagnosisResult> MetricsSeverityDiagnosis { get; set; }
        public required IEnumerable<PathogenResult> WorthSuspected { get; set; }
        public required IEnumerable<PathogenResult> HeavySuspected { get; set; }
        public required IEnumerable<Evidence> Evidences { get; set; }
        public required IEnumerable<ClinicalVariableResult> MissingVariables { get; set; }
        public required IEnumerable<AntibioticResult> Allergies { get; set; }
        public required bool IsPatientPregnantOrInLactationPhase { get; set; }
    }
}
