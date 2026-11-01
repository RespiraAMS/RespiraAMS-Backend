using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Doctor.Application.Features.Doctors.Commands.SoftDelete
{
    public record SoftDeleteDoctorCommand : ICommand
    {
        public Guid SagaId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid TargetAccountId { get; set; }
        public Guid ActorAccountId { get; set; }
        public string ActorRole { get; set; } = string.Empty;
    }
}
