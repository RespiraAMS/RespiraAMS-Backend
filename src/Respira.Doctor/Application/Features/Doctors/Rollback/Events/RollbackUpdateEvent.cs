namespace Application.Features.Doctors.Rollback.Events
{
    public record RollbackUpdateDoctorSuccess
    {
        public Guid SagaId { get; init; }
        public Guid DoctorId { get; init; }
    }

    public record RollbackUpdateDoctorFailure
    {
        public Guid SagaId { get; init; }
        public Guid DoctorId { get; init; }
    }
}
