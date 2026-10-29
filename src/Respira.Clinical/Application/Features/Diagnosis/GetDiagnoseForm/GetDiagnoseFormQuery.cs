using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Application.Features.Diagnosis.GetDiagnoseForm
{
    public record GetDiagnoseFormQuery : IQuery;

    public record ClinicalVariableResult
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Code { get; set; }
        public required ClinicalValueType ValueType { get; set; }
        public string? CanonicalUnit { get; set; }
        public required bool IsRequired { get; set; }
        public required ClinicalVariableCategory Category { get; set; }
        public IEnumerable<string>? AcceptedValues { get; set; }
        public Range? NormalRange { get; set; }
        public Range? AcceptedRange { get; set; }
    }

    public record AntibioticResult
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required Guid AntibioticGroupId { get; set; }
        public required string AntibioticGroupName { get; set; }
    }

    public record PathogenResult
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
    }

    public record DiagnoseFormResult
    {
        public required IEnumerable<ClinicalVariableResult> ClinicalVariables { get; set; }
        public required IEnumerable<AntibioticResult> Antibiotics { get; set; }
        public required IEnumerable<PathogenResult> Pathogens { get; set; }
    }
}
