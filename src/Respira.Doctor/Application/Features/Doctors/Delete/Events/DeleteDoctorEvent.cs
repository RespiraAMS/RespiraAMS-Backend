namespace Application.Features.Doctors.Delete.Events
{
    public record DeleteDoctorSuccess
    {
        public required Guid SagaId { get; init; }
        public required Guid DoctorId { get; init; }
    }

    public record DeleteDoctorFailure
    {
        public required Guid SagaId { get; init; }
        public required Guid DoctorId { get; init; }
        public required string Message { get; init; }
    }
}
