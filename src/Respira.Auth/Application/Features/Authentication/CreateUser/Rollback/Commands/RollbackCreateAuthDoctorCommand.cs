using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Authentication.CreateUser.Rollback.Commands;

/// <summary>
/// Compensates a previously created AuthDoctor account when a later saga step fails.
/// </summary>
    public record RollbackCreateAuthDoctorCommand : ICommand
    {
        /// <summary>Correlation identifier of the CreateUser saga.</summary>
        public required Guid SagaId { get; init; }

        /// <summary>Identifier of the auth doctor account to remove.</summary>
        public required Guid AuthUserId { get; init; }
    }
