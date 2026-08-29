using Wolverine.Attributes;

namespace Application.Features.Diagnose.ValidateDiagnosis;

public class AntibioticRecord
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Classification { get; set; }
    public required string RouteOfAdministration { get; set; }
    public required string Dose { get; set; }
}

public class PathogenRecord
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}

[MessageIdentity("validate-diagnosis-query")]
public class ValidateDiagnosisQuery : IQuery
{
    public required List<AntibioticRecord> Antibiotics { get; set; }
    public required List<PathogenRecord> Pathogens { get; set; }
    public string? Severity { get; set; }
    public string? TreatmentSite { get; set; }
}

[MessageIdentity("validate-diagnosis-result")]
public class ValidateDiagnosisResult(bool isValid)
{
    public bool IsValid { get; set; } = isValid;
}
