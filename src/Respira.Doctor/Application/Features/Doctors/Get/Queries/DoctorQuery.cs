using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Doctors.Get.Queries
{
    public record DoctorQuery : IQuery
    {
        public Guid Id { get; set; }
    }
}
