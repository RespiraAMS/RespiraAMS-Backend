using Domain.Enums;

namespace Application.Features.Doctors.Update.Events
{
    public record UpdateDoctorSuccessEvent
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
    }

    public record UpdateDoctorFailureEvent
    {
        public Guid SagaId { get; init; }
        public Guid DoctorId { get; init; }
    }
}
