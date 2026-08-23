using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Doctors.GetDoctorInfo.Queries
{
    /// <summary>
    /// Retrieves a doctor's profile by ID. Uses cache-aside pattern.
    /// </summary>
    public record GetDoctorInfoQuery : IQuery
    {
        /// <summary>Identifier of the doctor to retrieve</summary>
        public required Guid DoctorId { get; set; }
    }
}
