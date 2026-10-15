using Respira.Patient.Application.Features.Patients.UpdatePatient;
using Respira.Patient.Domain.Enums;

namespace Respira.Patient.API.Dtos
{
    public record UpdatePatientRequestDto
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string PhoneNumber { get; set; }
        public required DateOnly DateOfBirth { get; set; }
        public required Gender Gender { get; set; }
        public required decimal Height { get; set; }
        public required decimal Weight { get; set; }
        public required string MedicalRecordCode { get; set; }
        public required string HealthInsuranceCard { get; set; }
        public required string Address { get; set; }
        public required string City { get; set; }

        public UpdatePatientCommand ToCommand(Guid id)
        {
            return new UpdatePatientCommand
            {
                Id = id,
                FirstName = FirstName,
                LastName = LastName,
                PhoneNumber = PhoneNumber,
                DateOfBirth = DateOfBirth,
                Gender = Gender,
                Height = Height,
                Weight = Weight,
                MedicalRecordCode = MedicalRecordCode,
                HealthInsuranceCard = HealthInsuranceCard,
                Address = Address,
                City = City,
            };
        }
    }
}
