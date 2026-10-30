using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Domain.Services
{
    public interface IDiagnoseService
    {
        Result<bool> IsMedicineCombinationValid(ClinicalContext context, MedicineComposition combination);
        Result<Diagnosis> Diagnose(ClinicalContext context, IEnumerable<ClinicalObservation> observations);
        Result<IEnumerable<Antibiogram>> GetMicrobiologicalTestResult(ClinicalContext context, IEnumerable<MicrobialSample> samples);
        Result<IEnumerable<Treatment>> RecommendTreatment(ClinicalContext context, ClinicalPicture clinicalPicture, Diagnosis diagnosis);
    }
}
