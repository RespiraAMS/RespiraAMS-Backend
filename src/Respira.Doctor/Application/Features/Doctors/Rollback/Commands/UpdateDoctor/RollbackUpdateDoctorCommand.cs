using Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Application.Features.Doctors.Rollback.Commands.UpdateDoctor
{
    /// <summary>
    /// Compensates a failed UpdateDoctor step by reverting the profile to its previous values.
    /// </summary>
    public record RollbackUpdateDoctorCommand : ICommand
    {
        /// <summary>Saga identifier coordinating the rollback</summary>
        public Guid SagaId { get; init; }

        /// <summary>Identifier of the doctor profile to revert</summary>
        public Guid DoctorId { get; init; }

        /// <summary>Previous first name</summary>
        public required string FirstName { get; init; }

        /// <summary>Previous last name</summary>
        public required string LastName { get; init; }

        /// <summary>Previous academic degrees</summary>
        public required List<DegreeType> Degrees { get; init; }

        /// <summary>Previous academic title</summary>
        public required AcademicTitleEnum AcademicTitle { get; init; }

        /// <summary>Previous hospital position</summary>
        public required PositionType Position { get; init; }

        /// <summary>Previous gender</summary>
        public GenderType Gender { get; init; }

        /// <summary>Previous citizen identification number</summary>
        public required string CitizenIdentificationNumber { get; init; }

        /// <summary>Previous date of birth (if any)</summary>
        public DateTimeOffset? DateOfBirth { get; init; }

        /// <summary>Previous address</summary>
        public required string Address { get; init; }

        /// <summary>Previous avatar media identifier (if any)</summary>
        public Guid? MediaId { get; init; }

        /// <summary>Identifier of the doctor's creator (if any)</summary>
        public Guid? DoctorCreatorId { get; init; }
    }
}
