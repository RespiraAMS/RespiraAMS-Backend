using Microsoft.EntityFrameworkCore;
using Respira.Patient.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Features.TreatmentPlans.GetTreatmentPlanById
{
    public class GetTreatmentPlanByIdHandler(IDbContext context)
        : IQueryHandler<GetTreatmentPlanByIdQuery, Result<TreatmentPlanResult>>
    {
        public async Task<Result<TreatmentPlanResult>> HandleAsync(GetTreatmentPlanByIdQuery query, CancellationToken cancellationToken = default)
        {
            var plan = await context.TreatmentPlans
                .AsNoTracking()
                .Select(x => new TreatmentPlanResult
                {
                    Id = x.Id,
                    Patient = new PatientResult(x.Patient.Id, x.Patient.FullName),
                    DoctorId = x.DoctorId,
                    Diagnosis = x.Diagnosis,
                    Medicines = x.Medicines,
                    ReasonForMedicineChoice = x.ReasonForMedicineChoice,
                    AdherenceState = x.AdherenceState,
                    Status = x.Status,
                    CloseReason = x.CloseReason
                })
                .FirstOrDefaultAsync(x => x.Id == query.Id);
            return plan is null ?
                Result<TreatmentPlanResult>.Failure(new ApplicationError(ApplicationStatus.ResourceNotFound, "Treatment plan not found")) :
                Result<TreatmentPlanResult>.Success(ApplicationStatus.Success, plan);
        }
    }
}
