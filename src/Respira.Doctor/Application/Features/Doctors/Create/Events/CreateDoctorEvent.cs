namespace Application.Features.Doctors.Create.Events
{
    /// <summary>
    /// Published after a doctor profile is successfully created.
    /// </summary>
    public record CreateDoctorSuccess
    {
        public required Guid SagaId { get; set; }
        public required Guid DoctorId { get; set; }
    }

    /// <summary>
    /// Published when doctor profile creation fails.
    /// </summary>
    public record CreateDoctorFailure
    {
        public required Guid SagaId { get; set; }
        public required string Message { get; set; }
    }
}
