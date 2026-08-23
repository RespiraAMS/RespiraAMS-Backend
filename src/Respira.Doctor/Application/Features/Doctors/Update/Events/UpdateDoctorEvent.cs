using Domain.Enums;

namespace Application.Features.Doctors.Update.Events
{
    /// <summary>
    /// Published after a doctor profile is successfully updated, carrying the new field values.
    /// </summary>
    public record UpdateDoctorSuccessEvent
    {
        /// <summary>Saga identifier</summary>
        public Guid SagaId { get; init; }

        /// <summary>Identifier of the updated doctor profile</summary>
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

        /// <summary>Updated date of birth (if any)</summary>
        public DateTimeOffset? DateOfBirth { get; init; }

        /// <summary>Updated address</summary>
        public required string Address { get; init; }

        /// <summary>Updated avatar media identifier (if any)</summary>
        public Guid? MediaId { get; init; }
    }

    /// <summary>
    /// Published when doctor profile update fails.
    /// </summary>
    public record UpdateDoctorFailureEvent
    {
        /// <summary>Saga identifier</summary>
        public Guid SagaId { get; init; }

        /// <summary>Identifier of the doctor profile that failed to update</summary>
        public Guid DoctorId { get; init; }
    }
}
