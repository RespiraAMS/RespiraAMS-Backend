using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.ServiceDefaults.Contracts.Messages.Accounts
{
    /// <summary>
    /// Query sent by other microservices to resolve account details by identifier.
    /// </summary>
    public record GetAccountsByIdsQuery : IQuery
    {
        /// <summary>Identifiers of the accounts to resolve.</summary>
        public Guid[] AccountIds { get; init; } = [];
    }

    /// <summary>
    /// Account data shared between microservices.
    /// </summary>
    public record AccountSummary
    {
        /// <summary>Account identifier.</summary>
        public Guid Id { get; init; }

        /// <summary>Email used as the unique login identifier.</summary>
        public required string Email { get; init; }

        /// <summary>Contact phone number.</summary>
        public required string Phone { get; init; }

        /// <summary>Role of the account (Doctor, HeadDepartmentDoctor, MedicalRegulationReviewer, Admin).</summary>
        public required string Role { get; init; }

        /// <summary>Account status (Active / Inactive).</summary>
        public required string Status { get; init; }
    }
}
