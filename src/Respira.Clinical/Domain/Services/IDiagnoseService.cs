using Respira.Domain.Entities;
using Respira.Domain.Enums;
using Respira.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Domain.Services
{
    public interface IDiagnoseService
    {
        decimal CalculateMetricsScore(ScoreMetrics metrics, IEnumerable<ClinicalObservation> observations);
        Result<SeverityDiagnosis> DiagnoseSeverity(ClinicalContext context, IEnumerable<ClinicalObservation> observations);
        Result<InfectionAssessment> AssessInfection(ClinicalContext context, IEnumerable<ClinicalObservation> observations, Severity severity, TreatmentSite treatmentSite);
    }
}
