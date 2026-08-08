using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Patients.GetPatientById;

public class GetPatientByIdHandler(IDbContext context) : IQueryHandler<GetPatientByIdQuery, PatientResult>
{
    public async Task<PatientResult> HandleAsync(GetPatientByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var patient = await context.Patients
            .AsNoTracking()
            .Select(x => new PatientResult
            {
                Id = x.Id,
                FullName = x.FullName,
                DateOfBirth = x.DateOfBirth,
                IsMale = x.IsMale,
                MedicalRecordCode = x.MedicalRecordCode,
                HealthInsuranceCardNumber = x.HealthInsuranceCardNumber,
                Address = x.Address,
                Admission = x.Admission,
                Discharge = x.Discharge,
                Status = x.Status,
                Treatments = x.Treatments.OrderBy(t => t.CreatedAt).Select(t => new TreatmentResult
                {
                    Id = t.Id,
                    Start = t.CreatedAt,
                    TreatmentType = t.TreatmentType,
                    Status = t.Status
                }).ToList()
            })
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);

        return patient ?? throw new NotFoundException(nameof(Patient), query.Id);
    }
}