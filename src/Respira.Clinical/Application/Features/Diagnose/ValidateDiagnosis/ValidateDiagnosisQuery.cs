using Wolverine.Attributes;

namespace Application.Features.Diagnose.ValidateDiagnosis;

public record AntibioticRecord
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Classification { get; set; }
    public required string RouteOfAdministration { get; set; }
    public required string Dose { get; set; }
}

public record PathogenRecord
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}

[MessageIdentity("validate-diagnosis-query")]
public record ValidateDiagnosisQuery : IQuery
{
    public required List<AntibioticRecord> Antibiotics { get; set; }
    public required List<PathogenRecord> Pathogens { get; set; }
    public string? Severity { get; set; }
    public string? TreatmentSite { get; set; }
}

[MessageIdentity("validate-diagnosis-result")]
public record ValidateDiagnosisResult(bool IsValid, string? Message = null)
{
    public bool IsValid { get; set; } = IsValid;
    public string? Message { get; set; } = Message;
}
