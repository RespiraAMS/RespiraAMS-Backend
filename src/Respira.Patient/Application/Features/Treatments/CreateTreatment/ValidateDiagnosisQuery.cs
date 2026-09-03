using Wolverine.Attributes;

/*
 * These classes are used for service to service communication via
 * RabbitMQ. Since we use Wolverine mediator with the MessageIdentity
 * attribute, we can just create a duplicate class with same attributes
 * instead of reference the same CLR classes
 */

namespace Application.Features.Treatments.CreateTreatment;

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
public class ValidateDiagnosisResult(bool isValid, string? message = null)
{
    public bool IsValid { get; set; } = isValid;
    public string? Message { get; set; } = message;
}
