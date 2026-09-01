using Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Application.Features.Doctors.Create.Commands
{
    /// <summary>
    /// Creates a new doctor profile as part of the CreateDoctor saga.
    /// </summary>
    public record CreateDoctorCommand : ICommand
    {
        /// <summary>Saga identifier coordinating the doctor creation</summary>
        public required Guid SagaId { get; set; }

        /// <summary>Identifier to assign to the new doctor profile</summary>
        public required Guid DoctorId { get; set; }

        /// <summary>Doctor first name</summary>
        public required string FirstName { get; set; }

        /// <summary>Doctor last name</summary>
        public required string LastName { get; set; }

        /// <summary>Academic degrees held by the doctor</summary>
        public required List<DegreeType> Degrees { get; set; }

        /// <summary>Academic title</summary>
        public required AcademicTitleEnum AcademicTitle { get; set; }

        /// <summary>Hospital position</summary>
        public required PositionType Position { get; set; }

        /// <summary>Gender</summary>
        public GenderType Gender { get; set; }

        /// <summary>Vietnam citizen identification number (CCCD/CMND)</summary>
        public required string CitizenIdentificationNumber { get; set; }

        /// <summary>Date of birth (if provided)</summary>
        public DateTimeOffset? DateOfBirth { get; set; }

        /// <summary>Residential address</summary>
        public required string Address { get; set; }

        /// <summary>Avatar media identifier (if uploaded)</summary>
        public Guid? MediaId { get; set; }

        /// <summary>Identifier of the doctor who created this profile (if any)</summary>
        public Guid? DoctorCreatorId { get; set; }
    }
}
