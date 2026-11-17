using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine.Attributes;

namespace Respira.Doctor.Application.Features.Doctors.Commands.Update
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
