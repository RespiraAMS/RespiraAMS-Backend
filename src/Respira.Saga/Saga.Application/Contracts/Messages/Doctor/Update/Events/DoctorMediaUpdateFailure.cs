using Wolverine.Attributes;

namespace Respira.Saga.Application.Contracts.Messages.Doctor.Update.Events
{
    [MessageIdentity("doctor-media-update-failure")]
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
