using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Saga.Application.Constracts.Messages.Doctor.Update
{
    public record UpdateDoctorMediaIdCommand : ICommand
    {
        public Guid SagaId { get; set; }
        public Guid ActorAccountId { get; set; }
        public string ActorRole { get; set; } = string.Empty;
        public Guid DoctorId { get; set; }
        public Guid MediaId { get; set; }
    }
}
