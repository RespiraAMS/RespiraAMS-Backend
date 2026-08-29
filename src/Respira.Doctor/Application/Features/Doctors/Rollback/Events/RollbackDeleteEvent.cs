namespace Application.Features.Doctors.Rollback.Events
{
    /// <summary>
    /// Published when the DeleteDoctor rollback (restore) succeeds.
    /// </summary>
    public record RollbackDeleteDoctorSuccess
    {
        /// <summary>Saga identifier</summary>
        public Guid SagaId { get; init; }

        /// <summary>Identifier of the doctor profile that was restored</summary>
        public Guid DoctorId { get; init; }
    }

    /// <summary>
    /// Published when the DeleteDoctor rollback fails.
    /// </summary>
    public record RollbackDeleteDoctorFailure
    {
        /// <summary>Saga identifier</summary>
        public Guid SagaId { get; init; }

        /// <summary>Identifier of the doctor profile that failed to restore</summary>
        public Guid DoctorId { get; init; }
    }
}
