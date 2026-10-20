using Microsoft.EntityFrameworkCore;
using Respira.Patient.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Features.Patients.GetPatientById
{
    public class GetPatientByIdHandler(IDbContext context) : IQueryHandler<GetPatientByIdQuery, Result<PatientResult>>
    {
        public async Task<Result<PatientResult>> HandleAsync(GetPatientByIdQuery query, CancellationToken cancellationToken = default)
        {
            var patient = await context.Patients
                .AsNoTracking()
                .Select(x => new PatientResult
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    PhoneNumber = x.PhoneNumber,
                    DateOfBirth = x.DateOfBirth,
                    Gender = x.Gender,
                    Height = x.Height,
                    Weight = x.Weight,
                    MedicalRecordCode = x.MedicalRecordCode,
                    HealthInsuranceCard = x.HealthInsuranceCard,
                    Address = x.Address,
                    City = x.City,
                    Admission = x.Admission,
                    Discharge = x.Discharge,
                    TreatmentOutcome = x.TreatmentOutcome,
                    NextOfKins = x.NextOfKins.Select(nok => new NextOfKinItem(
                        nok.Id,
                        nok.FullName,
                        nok.PhoneNumber,
                        nok.Relationship
                    )),
                    Treatments = x.Treatments.Select(t => new TreatmentItem(t.Id))
                })
                .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);

            return patient is null
                ? Result<PatientResult>.Failure(new ApplicationError(ApplicationStatus.ResourceNotFound, "Patient not found"))
                : Result<PatientResult>.Success(ApplicationStatus.Success, patient);
        }
    }
}
