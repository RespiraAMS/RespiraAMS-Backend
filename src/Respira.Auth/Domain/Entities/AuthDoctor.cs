using Domain.Enums;
using Respira.ServiceDefaults.Models;

namespace Domain.Entities
{
    /// <summary>
    /// A registered doctor (or staff) account that can authenticate with the system.
    /// </summary>
    public class AuthDoctor : Base
    {
        /// <summary>Email used as the unique login identifier (lowercased on use)</summary>
        public required string Email { get; set; }

        /// <summary>BCrypt hash of the account password</summary>
        public required string HashPassword { get; set; }

        /// <summary>Contact phone number</summary>
        public required string Phone { get; set; }

        /// <summary>Role of the account (Doctor, Manager, Admin)</summary>
        public RoleType Role { get; set; }

        /// <summary>Whether the email address has been confirmed via the verification link</summary>
        public bool IsEmailConfirmed { get; set; }

        /// <summary>Account status (Active / Inactive)</summary>
        public StatusType Status { get; set; }

        /// <summary>Tokens (refresh, verification, ...) issued for this account</summary>
        public ICollection<Token> Tokens { get; set; } = [];
    }
}
