using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Doctor.Application.Features.Doctors.Commands.Update
{
    public record UpdateDoctorMediaIdCommand : ICommand
    {
        public Guid SagaId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid MediaId { get; set; }
    }
}
