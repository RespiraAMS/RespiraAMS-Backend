using Domain.Enums;
using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Doctors.Update.Commands
{
    /// <summary>
    /// Updates a doctor's profile. Fields mirror the <see cref="Domain.Entities.Doctor"/> entity.
    /// </summary>
    public record UpdateDoctorCommand : ICommand
    {
        public Guid SagaId { get; init; }
        public required Guid DoctorId { get; init; }
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
