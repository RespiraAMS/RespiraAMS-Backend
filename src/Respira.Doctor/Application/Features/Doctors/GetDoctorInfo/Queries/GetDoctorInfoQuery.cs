using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Doctors.GetDoctorInfo.Queries
{
    public record GetDoctorInfoQuery : IQuery
    {
        public required Guid DoctorId { get; set; }
    }
}
