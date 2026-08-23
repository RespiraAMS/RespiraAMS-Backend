namespace Application.Features.Doctors.Rollback.Events
{
    /// <summary>
    /// Published when the UpdateDoctor rollback (revert) succeeds.
    /// </summary>
    public record RollbackUpdateDoctorSuccess
    {
        /// <summary>Saga identifier</summary>
        public Guid SagaId { get; init; }

        /// <summary>Identifier of the doctor profile that was reverted</summary>
        public Guid DoctorId { get; init; }
    }

    /// <summary>
    /// Published when the UpdateDoctor rollback fails.
    /// </summary>
    public record RollbackUpdateDoctorFailure
    {
        /// <summary>Saga identifier</summary>
        public Guid SagaId { get; init; }

        /// <summary>Identifier of the doctor profile that failed to revert</summary>
        public Guid DoctorId { get; init; }
    }
}
