namespace Application.Features.Doctors.Create.Events
{
    public record CreateDoctorSuccess
    {
        public required Guid SagaId { get; set; }
        public required Guid DoctorId { get; set; }
    }

    public record CreateDoctorFailure
    {
        public required Guid SagaId { get; set; }
        public required string Message { get; set; }
    }
}
