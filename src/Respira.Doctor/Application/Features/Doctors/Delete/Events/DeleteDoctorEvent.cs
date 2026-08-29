namespace Application.Features.Doctors.Delete.Events
{
    /// <summary>
    /// Published after a doctor profile is successfully (soft-)deleted.
    /// </summary>
    public record DeleteDoctorSuccess
    {
        /// <summary>Saga identifier</summary>
        public required Guid SagaId { get; init; }

        /// <summary>Identifier of the deleted doctor profile</summary>
        public required Guid DoctorId { get; init; }
    }

    /// <summary>
    /// Published when doctor profile deletion fails.
    /// </summary>
    public record DeleteDoctorFailure
    {
        /// <summary>Saga identifier</summary>
        public required Guid SagaId { get; init; }

        /// <summary>Identifier of the doctor profile that failed to delete</summary>
        public required Guid DoctorId { get; init; }

        /// <summary>Failure message describing why deletion failed</summary>
        public required string Message { get; init; }
    }
}
