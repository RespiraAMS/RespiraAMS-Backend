using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Doctors.Delete.Commands
{
    /// <summary>
    /// Soft-deletes a doctor profile. Part of the DeleteDoctor saga.
    /// </summary>
    public record DeleteDoctorCommand : ICommand
    {
        public required Guid SagaId { get; init; }
        public required Guid DoctorId { get; init; }
        public Guid? DoctorCreatorId { get; init; }
    }
}
