namespace Application.Features.Doctors.LinkAvatar.Events
{
    public record LinkDoctorAvatarSuccessEvent
    {
        public required Guid SagaId { get; init; }
        public required Guid DoctorId { get; init; }
        public Guid? MediaId { get; init; }
    }

    public record LinkDoctorAvatarFailureEvent
    {
        public required Guid SagaId { get; init; }
        public required Guid DoctorId { get; init; }
    }
}
