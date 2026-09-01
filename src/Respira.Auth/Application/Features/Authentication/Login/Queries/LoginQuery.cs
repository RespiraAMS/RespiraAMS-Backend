using Respira.ServiceDefaults.Contracts.CQRS;

namespace Application.Features.Authentication.Login.Queries
{
    /// <summary>
    /// Query to authenticate a doctor with email and password.
    /// </summary>
    public record LoginQuery : IQuery
    {
        /// <summary>User email (login identifier)</summary>
        public required string Email { get; set; }

        /// <summary>Plain-text password to verify</summary>
        public required string Password { get; set; }
    }
}
