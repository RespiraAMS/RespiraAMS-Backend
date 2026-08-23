using Domain.Enums;
using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Doctors.Rollback.Commands.UpdateDoctor
{
    /// <summary>
    /// Compensates a failed UpdateDoctor step by reverting the profile to its previous values.
    /// </summary>
    public record RollbackUpdateDoctorCommand : ICommand
    {
        public Guid SagaId { get; init; }
        public Guid DoctorId { get; init; }
        public required string FirstName { get; init; }
        public required string LastName { get; init; }
        public required List<DegreeType> Degrees { get; init; }
        public required AcademicTitleEnum AcademicTitle { get; init; }
        public required PositionType Position { get; init; }
        public GenderType Gender { get; init; }
        public required string CitizenIdentificationNumber { get; init; }
        public DateTimeOffset? DateOfBirth { get; init; }
        public required string Address { get; init; }
        public Guid? MediaId { get; init; }
        public Guid? DoctorCreatorId { get; init; }
    }
}
