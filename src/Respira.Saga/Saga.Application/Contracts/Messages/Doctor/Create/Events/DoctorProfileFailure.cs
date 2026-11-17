using Wolverine.Attributes;

namespace Respira.Saga.Application.Contracts.Messages.Doctor.Create.Events
{
    [MessageIdentity("doctor-profile-failure")]
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
