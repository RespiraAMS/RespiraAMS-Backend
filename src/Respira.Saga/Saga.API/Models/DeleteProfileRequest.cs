namespace Respira.Saga.API.Models
{
    /// <summary>
    /// Payload of the profile deletion endpoint. This service owns no database, so the
    /// caller names the three things the flow removes.
    /// </summary>
    public record DeleteProfileRequest
    {
        /// <summary>Account to soft delete.</summary>
        public required Guid AccountId { get; set; }

        /// <summary>Doctor/manager profile to soft delete.</summary>
        public required Guid DoctorId { get; set; }

        /// <summary>Media asset of the avatar to delete; empty when there is none.</summary>
        public required Guid MediaId { get; set; }
    }
}
