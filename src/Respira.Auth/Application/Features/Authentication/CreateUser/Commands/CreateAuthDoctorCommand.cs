using Domain.Enums;
using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Authentication.CreateUser.Commands;

/// <summary>
/// Creates a new AuthDoctor account (login + role) as the first step of the
/// CreateUser saga. Correlated to the saga via <see cref="SagaId"/>.
/// </summary>
    public record CreateAuthDoctorCommand : ICommand
    {
        /// <summary>Correlation identifier of the CreateUser saga.</summary>
        public required Guid SagaId { get; init; }

        /// <summary>Identifier of the auth doctor account to create.</summary>
        public required Guid AuthUserId { get; init; }

        /// <summary>Email (login identifier) for the new account.</summary>
        public required string Email { get; init; }

        /// <summary>Plain-text password for the new account (hashed before storage).</summary>
        public required string Password { get; init; }

        /// <summary>Contact phone number for the new account.</summary>
        public required string Phone { get; init; }

        /// <summary>Role assigned to the new account.</summary>
        public required RoleType Role { get; init; }
    }
