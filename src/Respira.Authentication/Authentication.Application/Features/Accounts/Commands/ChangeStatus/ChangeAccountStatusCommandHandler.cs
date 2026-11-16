using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Respira.Authentication.Application.Contracts.Authentication;
using Respira.Authentication.Application.Contracts.Cache;
using Respira.Authentication.Application.Contracts.Data;
using Respira.Authentication.Application.Features.Authentication.Queries.Common;
using Respira.Authentication.Domain.Entities;
using Respira.Authentication.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Authentication.Application.Features.Accounts.Commands.ChangeStatus
{
    /// <summary>
    /// Handles locking and unlocking of accounts.
    /// When locked (Inactive), all active refresh tokens are revoked to terminate active sessions immediately.
    /// Soft-deleted accounts cannot be locked or unlocked.
    /// </summary>
    public class ChangeAccountStatusCommandHandler(
        IAuthDbContext dbContext,
        ICacheService cacheService,
        IOptions<JwtOption> jwtOption,
        ILogger<ChangeAccountStatusCommandHandler> logger,
        IMessageBus bus
    ) : ICommandHandler<ChangeAccountStatusCommand, Result<AccountResult>>
    {
        public async Task<Result<AccountResult>> HandleAsync(
            ChangeAccountStatusCommand command,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(command);

            if (command.TargetAccountId == Guid.Empty)
            {
                return Result<AccountResult>.Failure(
                    new ApplicationError(ApplicationStatus.BadRequest, "Account ID is required")
                );
            }

            // An admin cannot lock/deactivate their own account to prevent lockouts
            if (
                command.ActorAccountId.HasValue
                && command.ActorAccountId.Value == command.TargetAccountId
                && command.Status == StatusType.Inactive
            )
            {
                return Result<AccountResult>.Failure(
                    new ApplicationError(
                        ApplicationStatus.BadRequest,
                        "You cannot deactivate or lock your own account"
                    )
                );
            }

            // Target accounts that are not deleted (!IsDeleted)
            var account = await dbContext.Accounts.FirstOrDefaultAsync(
                x => x.Id == command.TargetAccountId && !x.IsDeleted,
                cancellationToken
            );

            if (account is null)
            {
                return Result<AccountResult>.Failure(
                    new ApplicationError(
                        ApplicationStatus.ResourceNotFound,
                        "Account not found or has been deleted"
                    )
                );
            }

            var oldStatus = account.Status;

            // Only update if the status is actually changed
            if (account.Status != command.Status)
            {
                account.Status = command.Status;
                account.UpdatedAt = DateTime.UtcNow;

                // When locking (Inactive), revoke all active refresh tokens for this account
                var hasRefreshTokens = false;
                if (command.Status == StatusType.Inactive)
                {
                    var refreshTokens = await dbContext
                        .Tokens.AsNoTracking()
                        .Where(x =>
                            x.AccountId == account.Id && x.TokenType == TokenType.RefreshToken
                        )
                        .ToListAsync(cancellationToken);

                    if (refreshTokens.Count > 0)
                    {
                        hasRefreshTokens = true;
                        var expirationDate = DateTimeOffset.UtcNow.AddDays(jwtOption.Value.RefreshTokenExpires);
                        foreach (var refreshToken in refreshTokens)
                        {
                            dbContext.BlacklistTokens.Add(
                                new BlacklistToken
                                {
                                    HashToken = refreshToken.HashToken,
                                    Reason = "Account locked by administrator",
                                    ExpirationDate = refreshToken.ExpirationDate ?? expirationDate,
                                }
                            );
                        }
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                if (hasRefreshTokens)
                {
                    // Set-based DELETE: a request that already revoked these rows affects
                    // 0 rows instead of throwing DbUpdateConcurrencyException. The account
                    // is already Inactive, so its sessions are rejected regardless.
                    await dbContext
                        .Tokens.Where(x =>
                            x.AccountId == account.Id && x.TokenType == TokenType.RefreshToken
                        )
                        .ExecuteDeleteAsync(cancellationToken);
                }

                // Clear cached entries for this account
                await cacheService.RemoveAsync(
                    $"auth:account:email:{account.Email.ToLowerInvariant()}",
                    cancellationToken
                );
                await cacheService.RemoveAsync(
                    $"auth:account:id:{account.Id}",
                    cancellationToken
                );
                await cacheService.RemoveAsync(
                    $"auth:accounts:{account.Email}",
                    cancellationToken
                );

                await bus.PublishAsync(
                    new AuditEvent
                    {
                        ActorAccountId = command.ActorAccountId,
                        ActorRole = string.IsNullOrWhiteSpace(command.ActorRole) ? "Admin" : command.ActorRole,
                        Action = command.Status == StatusType.Inactive
                            ? "Authentication.AccountLocked"
                            : "Authentication.AccountUnlocked",
                        EntityType = "Account",
                        EntityId = account.Id,
                        DetailsJson = $"{{\"previousStatus\":\"{oldStatus}\",\"newStatus\":\"{command.Status}\"}}",
                    }
                );

                logger.LogInformation(
                    "Account {AccountId} status updated from {OldStatus} to {NewStatus} by {ActorRole} ({ActorAccountId})",
                    account.Id,
                    oldStatus,
                    command.Status,
                    command.ActorRole,
                    command.ActorAccountId
                );
            }

            var result = new AccountResult
            {
                Id = account.Id,
                Email = account.Email,
                Phone = account.Phone,
                Role = account.Role,
                IsEmailConfirmed = account.IsEmailConfirmed,
                Status = account.Status,
            };

            return Result<AccountResult>.Success(ApplicationStatus.Success, result);
        }
    }
}
