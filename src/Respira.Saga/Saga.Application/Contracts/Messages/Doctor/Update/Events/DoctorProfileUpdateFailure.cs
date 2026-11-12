using Wolverine.Attributes;

namespace Respira.Saga.Application.Contracts.Messages.Doctor.Update.Events
{
    [MessageIdentity("doctor-profile-update-failure")]
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
