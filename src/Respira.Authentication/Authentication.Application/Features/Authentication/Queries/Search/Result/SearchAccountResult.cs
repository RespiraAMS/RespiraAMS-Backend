using Authentication.Domain.Enums;

namespace Authentication.Application.Features.Authentication.Queries.Search.Result
{
    public record AccountResult
    {
        /// <summary>
        /// Unique identifier for the account
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>Email used as the unique login identifier (lowercased on use)</summary>
        public required string Email { get; set; }

        /// <summary>Contact phone number</summary>
        public required string Phone { get; set; }

        /// <summary>Role of the account (Doctor, Manager, Admin)</summary>
        public RoleType Role { get; set; }

        /// <summary>Whether the email address has been confirmed via the verification link</summary>
        public bool IsEmailConfirmed { get; set; }

        /// <summary>Account status (Active / Inactive)</summary>
        public StatusType Status { get; set; }
    }
}
