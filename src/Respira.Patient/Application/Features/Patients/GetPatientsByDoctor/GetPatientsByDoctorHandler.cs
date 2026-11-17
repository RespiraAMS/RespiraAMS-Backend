using Microsoft.EntityFrameworkCore;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Messages.Patients;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Features.Patients.GetPatientsByDoctor;

/// <summary>Returns patients with matching closed treatment plans in the requested period.</summary>
public sealed class GetPatientsByDoctorHandler(IDbContext context)
    : IQueryHandler<GetPatientsByDoctorQuery, Result<PatientsByDoctorResult>>
{
    public async Task<Result<PatientsByDoctorResult>> HandleAsync(
        GetPatientsByDoctorQuery query,
        CancellationToken cancellationToken = default
    )
    {
        if (query.DoctorId == Guid.Empty || query.End <= query.Start)
        {
            return Result<PatientsByDoctorResult>.Success(
                ApplicationStatus.Success,
                new PatientsByDoctorResult()
            );
        }

        var start = query.Start.ToUniversalTime();
        var end = query.End.ToUniversalTime();

        var patients = await context
            .Patients.AsNoTracking()
            .AsSplitQuery()
            .Where(patient =>
                !patient.IsDeleted
                && patient.TreatmentPlans.Any(plan =>
                    !plan.IsDeleted
                    && (query.DoctorId == null || plan.DoctorId == query.DoctorId)
                    && plan.UpdatedAt >= start
                    && plan.UpdatedAt < end
                    && plan.Status == TreatmentPlanStatus.Closed
                )
            )
            .Include(patient => patient.NextOfKins.Where(nextOfKin => !nextOfKin.IsDeleted))
            .Include(patient =>
                patient.TreatmentPlans.Where(plan =>
                    !plan.IsDeleted
                    && (query.DoctorId == null || plan.DoctorId == query.DoctorId)
                    && plan.UpdatedAt >= start
                    && plan.UpdatedAt < end
                    && plan.Status == TreatmentPlanStatus.Closed
                )
            )
            .ToListAsync(cancellationToken);

        var resultPatients = patients.Select(patient => new PatientAnalysisItem
        {
            Id = patient.Id,
            FullName = patient.FullName,
            PhoneNumber = patient.PhoneNumber,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender.ToString(),
            Height = patient.Height,
            Weight = patient.Weight,
            MedicalRecordCode = patient.MedicalRecordCode,
            HealthInsuranceCard = patient.HealthInsuranceCard,
            Address = patient.Address,
            City = patient.City,
            Admission = patient.Admission,
            Discharge = patient.Discharge,
            TreatmentOutcome = patient.TreatmentOutcome.ToString(),
                NextOfKins = patient
                    .NextOfKins.Select(nextOfKin => new NextOfKinAnalysisItem(
                        nextOfKin.Id,
                        nextOfKin.FullName,
                        nextOfKin.PhoneNumber,
                        nextOfKin.Relationship
                    ))
                    .ToList(),
                TreatmentPlans = patient
                    .TreatmentPlans.Select(plan => new TreatmentPlanAnalysisItem(
                        plan.Id,
                        plan.DoctorId,
                        plan.AdherenceState.ToString(),
                        plan.Status.ToString(),
                        plan.UpdatedAt,
                        plan.ReasonForMedicineChoice,
                        plan
                            .Medicines.Select(medicine => new AntibioticAnalysisItem(
                                medicine.Id,
                                medicine.Name,
                                medicine.Classification
                            ))
                            .ToList()
                    ))
                    .ToList(),
            })
            .ToList();

        return Result<PatientsByDoctorResult>.Success(
            ApplicationStatus.Success,
            new PatientsByDoctorResult
            {
                Patients = resultPatients,
            }
        );
    }

}
