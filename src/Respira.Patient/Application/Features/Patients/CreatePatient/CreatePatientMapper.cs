using Application.Features.Patients.Shared;
using Domain.Enums;

namespace Application.Features.Patients.CreatePatient;

public class CreatePatientMapper : ICreateMapper<Patient, CreatePatientCommand>
{
    public Patient ToModel(CreatePatientCommand command)
    {
        return new Patient
        {
            FullName = PatientNameNormalizer.Normalize(command.FullName),
            DateOfBirth = command.DateOfBirth,
            IsMale = command.IsMale,
            MedicalRecordCode = command.MedicalRecordCode,
            HealthInsuranceCardNumber = command.HealthInsuranceCardNumber,
            Address = command.Address,
            Admission = DateTimeOffset.UtcNow,
            Discharge = null,
            Status = PatientStatus.InTreatment
        };
    }
}