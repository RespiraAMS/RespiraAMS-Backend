using Respira.Patient.Application.Features.Patients.AddNextOfKin;
using Respira.Patient.Application.Features.Patients.UpdateNextOfKin;

namespace Respira.Patient.API.Dtos
{
    public record AddNextOfKinRequestDto
    {
        public required string FullName { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Relationship { get; set; }

        public AddNextOfKinCommand ToCommand(Guid patientId)
        {
            return new AddNextOfKinCommand
            {
                PatientId = patientId,
                FullName = FullName,
                PhoneNumber = PhoneNumber,
                Relationship = Relationship,
            };
        }
    }

    public record UpdateNextOfKinRequestDto
    {
        public required string FullName { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Relationship { get; set; }

        public UpdateNextOfKinCommand ToCommand(Guid nextOfKinId)
        {
            return new UpdateNextOfKinCommand
            {
                Id = nextOfKinId,
                FullName = FullName,
                PhoneNumber = PhoneNumber,
                Relationship = Relationship,
            };
        }
    }
}
