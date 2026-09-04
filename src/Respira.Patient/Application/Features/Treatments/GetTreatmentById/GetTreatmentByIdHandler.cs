using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Application.Features.Treatments.GetTreatmentById
{
    public class GetTreatmentByIdHandler(IDbContext context, IMessageBus bus, ILogger<GetTreatmentByIdHandler> logger)
        : IQueryHandler<GetTreatmentByIdQuery, Result<TreatmentInfo>>
    {
        public async Task<Result<TreatmentInfo>> HandleAsync(GetTreatmentByIdQuery query, CancellationToken cancellationToken = default)
        {
            // Get treatment by ID. Technically, other doctor can still see the treatment even if they are
            // not responsible for this treatment (a patient can be treated by different doctors, and doctor
            // may need to look at past treatments for reference,...)
            var treatment = await context.Treatments
                .Include(x => x.Patient)
                .Where(x => x.Id == query.Id && x.PatientId == query.PatientId)
                .FirstOrDefaultAsync(cancellationToken);
            if (treatment is null)
            {
                logger.LogDebug("Treatment not found: {id}", query.Id);
                return Result<TreatmentInfo>.Failure(new Error(Status.ResourceNotFound, "Treatment not found"));
            }

            // Get doctor by ID
            var resp = await bus.InvokeAsync<ApiResponse<DoctorQueryResult>>(new GetDoctorByIdQuery(treatment.DoctorId));
            if (!resp.Success)
            {
                logger.LogWarning("Failed to get doctor information: {DoctorId}", treatment.DoctorId);
                logger.LogDebug(resp.Message);
                return Result<TreatmentInfo>.Failure(new Error(Status.ServerError, "Failed to get doctor information"));
            }
            var doctor = resp.Data!;

            return Result<TreatmentInfo>.Success(Status.Success, new TreatmentInfo
            {
                Id = treatment.Id,
                Doctor = new DoctorInfo
                {
                    Id = treatment.DoctorId,
                    DisplayName = doctor.FirstName + " " + doctor.LastName,
                    Avatar = doctor.Url,
                },
                Patient = new PatientInfo
                {
                    Id = treatment.PatientId,
                    FullName = treatment.Patient.FullName,
                    Age = treatment.Patient.Age(),
                    IsMale = treatment.Patient.IsMale,
                    MedicalRecordCode = treatment.Patient.MedicalRecordCode,
                    Status = treatment.Patient.Status,
                },
                Type = treatment.TreatmentType,
                Diagnosis = treatment.DiagnosisRecord,
            });
        }
    }
}
