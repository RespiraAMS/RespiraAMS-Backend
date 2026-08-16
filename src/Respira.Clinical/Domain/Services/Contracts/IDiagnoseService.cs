using Domain.Models;
using Domain.Services.Dtos;

namespace Domain.Services.Contracts;

/// <summary>
/// Contracts that all diagnosing service must compliance to
/// </summary>
public interface IDiagnoseService
{
    DiagnoseResult EmpiricalDiagnose(Disease disease, PatientInfo info, ClinicalPicture clinicalPicture);
    DiagnoseResult TargetedDiagnose(PatientInfo info, Antibiogram antibiogram);
}
