using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Doctors.Rollback.Commands.DeleteDoctor
{
    /// <summary>
    /// Compensates a failed DeleteDoctor step by restoring the soft-deleted doctor profile.
    /// </summary>
    public record RollbackDeleteDoctorCommand : ICommand
    {
        public required Guid SagaId { get; init; }
        public required Guid DoctorId { get; init; }
        public required Guid DoctorCreatorId { get; init; }
    }
}
