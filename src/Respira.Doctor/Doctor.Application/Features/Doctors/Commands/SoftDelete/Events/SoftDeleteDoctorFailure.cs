namespace Respira.Doctor.Application.Features.Doctors.Commands.SoftDelete.Events
{
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
