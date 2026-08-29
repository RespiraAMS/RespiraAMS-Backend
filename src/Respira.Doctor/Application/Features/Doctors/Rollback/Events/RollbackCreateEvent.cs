namespace Application.Features.Doctors.Rollback.Events
{
    /// <summary>
    /// Published when the CreateDoctor rollback (hard delete) succeeds.
    /// </summary>
    public record RollbackCreateDoctorSuccess
    {
        /// <summary>Saga identifier</summary>
        public Guid SagaId { get; init; }

        /// <summary>Identifier of the doctor profile that was rolled back</summary>
        public Guid DoctorId { get; init; }
    }

    /// <summary>
    /// Published when the CreateDoctor rollback fails.
    /// </summary>
    public record RollbackCreateDoctorFailure
    {
        /// <summary>Saga identifier</summary>
        public Guid SagaId { get; init; }

        /// <summary>Identifier of the doctor profile that failed to roll back</summary>
        public Guid DoctorId { get; init; }
    }
}
