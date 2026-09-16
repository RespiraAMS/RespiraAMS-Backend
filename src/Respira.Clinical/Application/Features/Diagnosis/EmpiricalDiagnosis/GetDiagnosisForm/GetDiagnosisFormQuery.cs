using Respira.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Application.Features.Diagnosis.EmpiricalDiagnosis.GetDiagnosisForm
{
    public class GetDiagnosisFormQuery : IQuery;

    public record ClinicalVariableResult
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Code { get; set; }
        public required ClinicalValueType ValueType { get; set; }
        public string? CanonicalUnit { get; set; }
    }

    public record GetDiagnosisFormResult
    {
        public required IEnumerable<ClinicalVariableResult> Variables { get; set; }
    }
}
