using Respira.Doctor.Domain.Enums;
using Wolverine.Attributes;

namespace Respira.Doctor.Application.Features.Doctors.Commands.Create.Events
{
    [MessageIdentity("doctor-profile-created")]
    public record DoctorProfileCreated
    {
        public DoctorProfileCreated(Guid sagaId, Guid doctorId, Guid accountId, ProfileRole role)
        {
            SagaId = sagaId;
            DoctorId = doctorId;
            AccountId = accountId;
            Role = role;
        }

        public Guid SagaId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid AccountId { get; set; }
        public ProfileRole Role { get; set; }
    }
}
