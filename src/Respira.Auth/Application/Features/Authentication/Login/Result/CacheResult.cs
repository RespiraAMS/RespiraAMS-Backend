using Domain.Enums;

namespace Application.Features.Authentication.Login.Result
{
    /// <summary>
    /// Compact projection of an <see cref="AuthDoctor"/> cached to speed up
    /// authentication and authorization checks.
    /// </summary>
    public class CacheResult
    {
        /// <summary>Account ID</summary>
        public required Guid Id { get; set; }

        /// <summary>Account email (lowercased)</summary>
        public required string Email { get; set; }

        /// <summary>Account role</summary>
        public required RoleType Role { get; set; }

        /// <summary>Whether the email is confirmed</summary>
        public required bool IsEmailConfirmed { get; set; }

        /// <summary>Account status</summary>
        public StatusType Status { get; set; }
    }
}
