using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Domain.Services
{
    public interface IDiagnoseService
    {
        Result<Diagnosis> Diagnose(ClinicalContext context, ClinicalPicture clinicalPicture);
    }
}
