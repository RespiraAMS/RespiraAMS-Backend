namespace Application.Features.Doctors.Create.Events
{
    /// <summary>
    /// Published after a doctor profile is successfully created.
    /// </summary>
    public record CreateDoctorSuccess
    {
        /// <summary>Saga identifier</summary>
        public required Guid SagaId { get; set; }

        /// <summary>Identifier of the created doctor profile</summary>
        public required Guid DoctorId { get; set; }
    }

    /// <summary>
    /// Published when doctor profile creation fails.
    /// </summary>
    public record CreateDoctorFailure
    {
        /// <summary>Saga identifier</summary>
        public required Guid SagaId { get; set; }

        /// <summary>Failure message describing why creation failed</summary>
        public required string Message { get; set; }
    }
}
