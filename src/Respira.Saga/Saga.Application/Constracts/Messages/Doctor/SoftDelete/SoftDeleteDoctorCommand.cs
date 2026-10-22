using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Saga.Application.Constracts.Messages.Doctor.SoftDelete
{
    public record SoftDeleteDoctorCommand : ICommand
    {
        public Guid SagaId { get; set; }
        public Guid DoctorId { get; set; }
    }
}
