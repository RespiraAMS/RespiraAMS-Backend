using Respira.Doctor.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Saga.Application.Constracts.Messages.Doctor.Update
{
    /// <summary>
    /// Updates the profile fields of a doctor/manager and points it at the freshly
    /// uploaded avatar in one step. The event it raises carries the previous media id so
    /// the saga can drop the old avatar afterwards.
    /// </summary>
    public record UpdateDoctorProfileCommand : ICommand
    {
        public Guid SagaId { get; set; }
        public Guid DoctorId { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required List<DegreeType> Degrees { get; set; }
        public AcademicTitleEnum AcademicTitle { get; set; }
        public PositionType Position { get; set; }
        public GenderType Gender { get; set; }
        public required string CitizenIdentificationNumber { get; set; }
        public DateTimeOffset? DateOfBirth { get; set; }
        public required string Address { get; set; }

        /// <summary>Identifier of the newly uploaded avatar.</summary>
        public Guid NewMediaId { get; set; }
    }
}
