using Wolverine.Attributes;

namespace Respira.Saga.Application.Contracts.Messages.Doctor.SoftDelete.Events
{
    [MessageIdentity("soft-delete-doctor-completed")]
    public record SoftDeleteDoctorCompleted
    {
        public SoftDeleteDoctorCompleted(Guid sagaId, Guid doctorId)
        {
            SagaId = sagaId;
            DoctorId = doctorId;
        }

        public Guid SagaId { get; set; }
        public Guid DoctorId { get; set; }
    }
}
