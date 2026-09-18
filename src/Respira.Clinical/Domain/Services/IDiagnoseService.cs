using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Domain.Services
{
    public interface IDiagnoseService
    {
        decimal CalculateMetricsScore(ScoreMetrics metrics, IEnumerable<ClinicalObservation> observations);
        Result<SeverityDiagnosis> DiagnoseSeverity(ClinicalContext context, IEnumerable<ClinicalObservation> observations);
        Result<InfectionAssessment> AssessInfection(ClinicalContext context, IEnumerable<ClinicalObservation> observations, Severity severity, TreatmentSite treatmentSite);
    }
}
