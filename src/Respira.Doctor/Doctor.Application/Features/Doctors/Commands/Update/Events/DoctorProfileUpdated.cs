namespace Respira.Doctor.Application.Features.Doctors.Commands.Update.Events
{
    public record DoctorProfileUpdated
    {
        public DoctorProfileUpdated(Guid sagaId, Guid doctorId, Guid mediaId, Guid oldMediaId)
        {
            SagaId = sagaId;
            DoctorId = doctorId;
            MediaId = mediaId;
            OldMediaId = oldMediaId;
        }

        public Guid SagaId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid MediaId { get; set; }
        public Guid OldMediaId { get; set; }
    }
}
