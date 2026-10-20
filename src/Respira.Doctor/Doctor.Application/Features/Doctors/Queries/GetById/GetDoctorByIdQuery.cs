using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Doctor.Application.Features.Doctors.Queries.GetById
{
    public record GetDoctorByIdQuery : IQuery
    {
        public Guid DoctorId { get; set; }
    }
}
