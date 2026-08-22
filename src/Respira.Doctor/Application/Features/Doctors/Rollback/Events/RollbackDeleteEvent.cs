namespace Application.Features.Doctors.Rollback.Events
{
    public record RollbackDeleteDoctorSuccess
    {
        public Guid SagaId { get; init; }
        public Guid DoctorId { get; init; }
    }

    public record RollbackDeleteDoctorFailure
    {
        public Guid SagaId { get; init; }
        public Guid DoctorId { get; init; }
    }
}
