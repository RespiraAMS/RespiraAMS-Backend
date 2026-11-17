using Respira.Patient.Application.Features.Patients.AddNextOfKin;
using Respira.Patient.Application.Features.Patients.UpdateNextOfKin;

namespace Respira.Patient.API.Dtos
{
    public record AddNextOfKinRequestDto
    {
        /// <summary>
        /// Next of kin full name
        /// </summary>
        public required string FullName { get; set; }

        /// <summary>
        /// Kin's phone number. This should be unique per patient,
        /// and should follow Vietnamese phone number format
        /// </summary>
        public required string PhoneNumber { get; set; }

        /// <summary>
        /// Kin's relationship with patient (parent, sibling,...)
        /// </summary>
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
        /// <summary>
        /// Next of kin full name
        /// </summary>
        public required string FullName { get; set; }

        /// <summary>
        /// Kin's phone number. This should be unique per patient,
        /// and should follow Vietnamese phone number format
        /// </summary>
        public required string PhoneNumber { get; set; }

        /// <summary>
        /// Kin's relationship with patient (parent, sibling,...)
        /// </summary>
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
