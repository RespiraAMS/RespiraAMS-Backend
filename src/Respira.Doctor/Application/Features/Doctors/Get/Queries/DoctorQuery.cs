using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine.Attributes;

namespace Application.Features.Doctors.Get.Queries
{
    [MessageIdentity("doctor-query")]
    public record DoctorQuery : IQuery
    {
        public Guid Id { get; set; }
    }
}
