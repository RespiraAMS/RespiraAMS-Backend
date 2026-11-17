using Wolverine.Attributes;

namespace Respira.Doctor.Application.Features.Doctors.Commands.Update.Events
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
