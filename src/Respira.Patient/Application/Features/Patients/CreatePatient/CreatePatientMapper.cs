using Respira.Patient.Application.Contracts.Mappers;
using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Features.Patients.CreatePatient
{
    public class CreatePatientMapper : ICreateMapper<CreatePatientCommand, Domain.Entities.Patient>
    {
        public Result<Domain.Entities.Patient> ToModel(CreatePatientCommand command)
        {
            return Result<Domain.Entities.Patient>.Success(ApplicationStatus.Success, new Domain.Entities.Patient
            {
                FirstName = command.FirstName,
                LastName = command.LastName,
                PhoneNumber = command.PhoneNumber,
                DateOfBirth = command.DateOfBirth,
                Gender = command.Gender,
                Height = command.Height,
                Weight = command.Weight,
                MedicalRecordCode = command.MedicalRecordCode,
                HealthInsuranceCard = command.HealthInsuranceCard,
                Address = command.Address,
                City = command.City,
                Admission = DateTimeOffset.UtcNow,
                Discharge = null,
                TreatmentOutcome = TreatmentOutcome.InTreatment,
            });
        }
    }
}
