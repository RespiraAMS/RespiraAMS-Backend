using Domain.Models;
using Domain.Services.Dtos;
using Respira.ServiceDefaults.Contracts.Results;

namespace Domain.Services.Contracts;

/// <summary>
/// Contracts that all diagnosing service must compliance to
/// </summary>
public interface IDiagnoseService
{
    Result<DiagnoseResult> EmpiricalDiagnose(Disease disease, PatientInfo info, ClinicalPicture clinicalPicture);
    Result<DiagnoseResult> TargetedDiagnose(PatientInfo info, Antibiogram antibiogram);
}
