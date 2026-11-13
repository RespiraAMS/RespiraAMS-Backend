using Wolverine.Attributes;

namespace Respira.Doctor.Application.Features.Doctors.Commands.SoftDelete.Events
{
    [MessageIdentity("soft-delete-doctor-failure")]
    public record SoftDeleteDoctorFailure
    {
        public SoftDeleteDoctorFailure(Guid sagaId, string reason)
        {
            SagaId = sagaId;
            Reason = reason;
        }

        public Guid SagaId { get; set; }
        public string? Reason { get; set; }
    }
}
