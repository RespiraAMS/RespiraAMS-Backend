using Respira.Domain.Entities;
using Respira.Domain.Enums;
using Respira.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Domain.Services
{
    public interface IDiagnoseService
    {
        decimal CalculateMetricsScore(ScoreMetrics metrics, IEnumerable<ClinicalObservation> observations);
        Result<SeverityDiagnosis> DiagnoseSeverity(IEnumerable<ClinicalObservation> observations);
        Result<InfectionProbability> InfectionProbability(Severity severity, TreatmentSite treatmentSite, IEnumerable<ClinicalObservation> observations);
    }
}
