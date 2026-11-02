namespace Respira.Saga.Application.Constracts.Messages.Doctor.Update.Events
{
    public record DoctorProfileUpdateFailure
    {
        public DoctorProfileUpdateFailure(Guid sagaId, string reason)
        {
            SagaId = sagaId;
            Reason = reason;
        }

        public Guid SagaId { get; set; }
        public string? Reason { get; set; }
    }
}
