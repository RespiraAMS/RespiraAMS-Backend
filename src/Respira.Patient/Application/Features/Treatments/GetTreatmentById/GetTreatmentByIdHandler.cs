using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Application.Features.Treatments.GetTreatmentById
{
    public class GetTreatmentByIdHandler(IDbContext context, IMessageBus bus, ILogger<GetTreatmentByIdHandler> logger)
        : IQueryHandler<GetTreatmentByIdQuery, TreatmentInfo>
    {
        public async Task<TreatmentInfo> HandleAsync(GetTreatmentByIdQuery query, CancellationToken cancellationToken = default)
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
                throw new NotFoundException(nameof(Treatment), query.Id);
            }

            // Get doctor by ID
            var resp = await bus.InvokeAsync<Result<DoctorQueryResult>>(new GetDoctorByIdQuery(treatment.DoctorId));
            if (!resp.Success)
            {
                logger.LogWarning("Failed to get doctor information: {DoctorId}", treatment.DoctorId);
                logger.LogDebug(resp.Message);
                throw new ServerException();
            }
            var doctor = resp.Data;
            if (doctor is null)
            {
                logger.LogWarning("Doctor unexpectedly null even when get doctor query success");
                throw new UnexpectedException("Doctor unexpectedly null even when get doctor query success");
            }

            return new TreatmentInfo
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
            };
        }
    }
}
