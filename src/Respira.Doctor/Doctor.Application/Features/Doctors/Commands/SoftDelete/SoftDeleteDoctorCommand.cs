using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Doctor.Application.Features.Doctors.Commands.SoftDelete
{
    public record SoftDeleteDoctorCommand : ICommand
    {
        public Guid SagaId { get; set; }
        public Guid DoctorId { get; set; }
    }
}
