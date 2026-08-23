using Domain.Enums;
using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Doctors.Update.Commands
{
    /// <summary>
    /// Updates a doctor's profile. Fields mirror the <see cref="Domain.Entities.Doctor"/> entity.
    /// </summary>
    public record UpdateDoctorCommand : ICommand
    {
        /// <summary>Saga identifier coordinating the doctor update</summary>
        public Guid SagaId { get; init; }

        /// <summary>Identifier of the doctor profile to update</summary>
        public required Guid DoctorId { get; init; }

        /// <summary>Updated first name</summary>
        public required string FirstName { get; init; }

        /// <summary>Updated last name</summary>
        public required string LastName { get; init; }

        /// <summary>Updated academic degrees</summary>
        public required List<DegreeType> Degrees { get; init; }

        /// <summary>Updated academic title</summary>
        public required AcademicTitleEnum AcademicTitle { get; init; }

        /// <summary>Updated hospital position</summary>
        public required PositionType Position { get; init; }

        /// <summary>Updated gender</summary>
        public GenderType Gender { get; init; }

        /// <summary>Updated citizen identification number</summary>
        public required string CitizenIdentificationNumber { get; init; }

        /// <summary>Updated date of birth (if provided)</summary>
        public DateTimeOffset? DateOfBirth { get; init; }

        /// <summary>Updated address</summary>
        public required string Address { get; init; }

        /// <summary>Updated avatar media identifier (if any)</summary>
        public Guid? MediaId { get; init; }

        /// <summary>Identifier of the doctor's creator (if any)</summary>
        public Guid? DoctorCreatorId { get; init; }
    }
}
