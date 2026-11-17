using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine.Attributes;

namespace Respira.Doctor.Application.Features.Doctors.Commands.SoftDelete
{
    [MessageIdentity("soft-delete-doctor-command")]
    public record SoftDeleteDoctorCommand : ICommand
    {
        public Guid SagaId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid TargetAccountId { get; set; }
        public Guid ActorAccountId { get; set; }
        public string ActorRole { get; set; } = string.Empty;
    }
}
