using Domain.Enums;
using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Authentication.UpdateUser.Commands;

/// <summary>
/// Updates an AuthDoctor's email/phone/role as part of the UpdateUser saga.
/// Old values are carried along so the saga can compensate by reverting.
/// </summary>
    public record UpdateAuthDoctorCommand : ICommand
    {
        /// <summary>Correlation identifier of the UpdateUser saga.</summary>
        public required Guid SagaId { get; init; }

        /// <summary>Identifier of the auth doctor account to update.</summary>
        public required Guid AuthUserId { get; init; }

        // New values
        /// <summary>New email for the account.</summary>
        public required string Email { get; init; }

        /// <summary>New contact phone number for the account.</summary>
        public required string Phone { get; init; }

        /// <summary>New role for the account.</summary>
        public required RoleType Role { get; init; }

        // Old values (used for compensation)
        /// <summary>Previous email, used to revert the account on compensation.</summary>
        public required string OldEmail { get; init; }

        /// <summary>Previous phone number, used to revert the account on compensation.</summary>
        public required string OldPhone { get; init; }

        /// <summary>Previous role, used to revert the account on compensation.</summary>
        public required RoleType OldRole { get; init; }
    }
