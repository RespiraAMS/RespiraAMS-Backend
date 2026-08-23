namespace Application.Features.Doctors.LinkAvatar.Events
{
    public record UpdateDoctorLinkAvatarSuccessEvent
    {
        public required Guid SagaId { get; init; }
        public required Guid DoctorId { get; init; }
        public Guid? MediaId { get; init; }
    }

    public record UpdateDoctorLinkAvatarFailureEvent
    {
        public required Guid SagaId { get; init; }
        public required Guid DoctorId { get; init; }
    }
}
