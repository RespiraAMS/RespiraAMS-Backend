namespace Application.Features.Doctors.Rollback.Events
{
    public record RollbackCreateDoctorSuccess
    {
        public Guid SagaId { get; init; }
        public Guid DoctorId { get; init; }
    }

    public record RollbackCreateDoctorFailure
    {
        public Guid SagaId { get; init; }
        public Guid DoctorId { get; init; }
    }
}
