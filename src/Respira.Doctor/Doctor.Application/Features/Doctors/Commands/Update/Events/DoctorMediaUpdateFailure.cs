namespace Respira.Doctor.Application.Features.Doctors.Commands.Update.Events
{
    public record DoctorMediaUpdateFailure
    {
        public DoctorMediaUpdateFailure(Guid sagaId, string reason)
        {
            SagaId = sagaId;
            Reason = reason;
        }

        public Guid SagaId { get; set; }
        public string? Reason { get; set; }
    }
}
