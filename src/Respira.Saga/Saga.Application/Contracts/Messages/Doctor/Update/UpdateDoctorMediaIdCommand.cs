using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine.Attributes;

namespace Respira.Saga.Application.Contracts.Messages.Doctor.Update
{
    [MessageIdentity("update-doctor-media-id-command")]
    public record UpdateDoctorMediaIdCommand : ICommand
    {
        public Guid SagaId { get; set; }
        public Guid ActorAccountId { get; set; }
        public string ActorRole { get; set; } = string.Empty;
        public Guid DoctorId { get; set; }
        public Guid MediaId { get; set; }
    }
}
