using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Doctors.Rollback.Commands.CreateDoctor
{
    public record RollbackCreateDoctorCommand : ICommand
    {
        public required Guid SagaId { get; init; }
        public required Guid DoctorId { get; init; }
    }
}
