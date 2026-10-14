namespace Respira.Doctor.Application.Features.Doctors.Commands.Update.Events
{
    public record DoctorMediaUpdated
    {
        public DoctorMediaUpdated(Guid sagaId, Guid doctorId, Guid mediaId)
        {
            SagaId = sagaId;
            DoctorId = doctorId;
            MediaId = mediaId;
        }

        public Guid SagaId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid MediaId { get; set; }
    }
}
