namespace Respira.Saga.Application.Contracts.Messages.Doctor.Create.Events
{
    public record DoctorProfileFailure
    {
        public DoctorProfileFailure(Guid sagaId, string reason)
        {
            SagaId = sagaId;
            Reason = reason;
        }

        public Guid SagaId { get; set; }
        public string? Reason { get; set; }
    }
}
