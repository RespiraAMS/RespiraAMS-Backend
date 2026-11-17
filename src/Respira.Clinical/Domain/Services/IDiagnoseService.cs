using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Domain.Services
{
    public interface IDiagnoseService
    {
        Result<bool> IsMedicineCombinationValid(ClinicalContext context, IEnumerable<Antibiotic> combination);
        Result ValidateObservations(ClinicalContext context, IEnumerable<ClinicalObservation> observations);
        Result<Diagnosis> Diagnose(ClinicalContext context, IEnumerable<ClinicalObservation> observations);
        Result<IEnumerable<MicrobialTestResult>> GetMicrobiologicalTestResult(ClinicalContext context, IEnumerable<MicrobialSample> samples);
        Result<IEnumerable<TreatmentProtocol>> RecommendTreatment(ClinicalContext context, ClinicalPicture clinicalPicture, Diagnosis diagnosis);
    }
}
