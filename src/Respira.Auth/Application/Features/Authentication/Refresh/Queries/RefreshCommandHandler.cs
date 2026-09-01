using System.Threading;
using Application.Abstracts.Authentication;
using Application.Abstracts.Data;
using Application.Features.Authentication.Login.Result;
using Application.Features.Tokens;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Dtos;
using Respira.ServiceDefaults.Exceptions;

namespace Application.Features.Authentication.Refresh.Queries;

/// <summary>
/// Validates the supplied refresh token, rotates it and issues a new access/refresh
/// token pair. The old refresh token is revoked via <see cref="TokenRevoker"/> (the same
/// mechanism used by <c>RemoveTokenCommand</code>), keeping the refresh-token lifecycle
/// consistent with logout revocation.
/// </summary>
/// <param name="jwtService">JWT validation/generation</param>
/// <param name="hashService">Token hashing utility</param>
/// <param name="dbContext">Auth database context</param>
/// <param name="tokenRevoker">Shared revocation logic (blacklist + remove)</param>
/// <param name="jwtOption">JWT configuration (expiry used for the new refresh token)</param>
/// <param name="logger">Logger</param>
public class RefreshCommandHandler(
    IJwtService jwtService,
    IHashService hashService,
    IAuthDbContext dbContext,
    TokenRevoker tokenRevoker,
    IOptions<JwtOption> jwtOption,
    ILogger<RefreshCommandHandler> logger
) : IQueryHandler<RefreshCommand, ApiResponse<LoginResult>>
{
    /// <summary>
    /// Validates the refresh token, persists a new refresh token and revokes the previous one.
    /// </summary>
    /// <param name="query">Refresh request holding the raw refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>API response with the new access/refresh tokens, or a failure response</returns>
    public async Task<ApiResponse<LoginResult>> HandleAsync(
        RefreshCommand query,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // 1. Validate the refresh token signature + token_type claim.
            (string email, RoleType role) refreshTokenClaims;
            try
            {
                refreshTokenClaims = await jwtService.ValidateRefreshToken(query.RefreshToken);
            }
            catch (UnauthorizedAccessException)
            {
                return ApiResponse<LoginResult>.Fail(
                    message: "Invalid refresh token",
                    statusCode: StatusCodes.Status401Unauthorized
                );
            }

            var (email, role) = refreshTokenClaims;

            // 2. The refresh token must exist as a persisted, non-expired row (a revoked
            //    refresh token had its row removed during logout/refresh rotation).
            var refreshHash = hashService.HashToken(query.RefreshToken);
            var storedToken = await dbContext.Tokens.FirstOrDefaultAsync(
                t => t.HashToken == refreshHash && t.TokenType == TokenType.RefreshToken,
                cancellationToken
            );
            if (storedToken?.IsExpired() != false)
            {
                return ApiResponse<LoginResult>.Fail(
                    message: "Invalid or expired refresh token",
                    statusCode: StatusCodes.Status401Unauthorized
                );
            }

            // 3. Issue new tokens.
            var accessToken = jwtService.GenerateToken(email, role, storedToken.AuthUserId.ToString());
            var refreshToken = jwtService.GenerateRefreshToken(email, role, storedToken.AuthUserId.ToString());

            // 4. Persist the new refresh token.
            var token = new Token
            {
                HashToken = hashService.HashToken(refreshToken),
                AuthUserId = storedToken.AuthUserId,
                TokenType = TokenType.RefreshToken,
                ExpirationDate = DateTimeOffset.UtcNow.AddMinutes(
                    jwtOption.Value.RefreshTokenExpired
                ),
            };
            await dbContext.Tokens.AddAsync(token, cancellationToken);

            // 5. Revoke the previous refresh token (and blacklist it) via the shared logic.
            await tokenRevoker.TryStageRevocationAsync(
                refreshHash,
                "refresh_token_rotation",
                cancellationToken
            );

            if (await dbContext.SaveChangesAsync() <= 0)
            {
                throw new ServerException();
            }

            return ApiResponse<LoginResult>.Ok(
                new LoginResult { AccessToken = accessToken, RefreshToken = refreshToken }
            );
        }
        catch (ServerException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to refresh token for email {Email}", query.RefreshToken);
            throw new ServerException();
        }
    }
}
