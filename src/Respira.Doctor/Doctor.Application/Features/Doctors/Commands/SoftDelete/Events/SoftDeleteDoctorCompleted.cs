namespace Respira.Doctor.Application.Features.Doctors.Commands.SoftDelete.Events
{
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
