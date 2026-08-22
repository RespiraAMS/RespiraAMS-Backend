using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Doctors.Rollback.Commands.DeleteDoctor
{
    public record RollbackDeleteDoctorCommand : ICommand
    {
        public required Guid SagaId { get; init; }
        public required Guid DoctorId { get; init; }
        public required Guid DoctorCreatorId { get; init; }
    }
}
