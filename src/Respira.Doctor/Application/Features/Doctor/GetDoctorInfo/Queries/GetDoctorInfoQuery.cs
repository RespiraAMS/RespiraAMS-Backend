using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Doctor.GetDoctorInfo.Queries
{
    public record GetDoctorInfoQuery : IQuery
    {
        public required Guid DoctorId { get; set; }
    }
}
