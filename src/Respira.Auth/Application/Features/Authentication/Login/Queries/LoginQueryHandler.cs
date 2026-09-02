using Application.Abstracts.Authentication;
using Application.Abstracts.Caching;
using Application.Abstracts.Data;
using Application.Features.Authentication.Login.Result;
using Application.Features.Tokens.Commands.CreateToken;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Dtos;
using Respira.ServiceDefaults.Exceptions;
using Wolverine;

namespace Application.Features.Authentication.Login.Queries
{
    /// <summary>
    /// Authenticates a doctor with email/password, returns JWT access and refresh tokens,
    /// caches the user and persists the refresh token
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="dbContext">Auth database context</param>
    /// <param name="cacheService">Cache for user info (by email and by ID)</param>
    /// <param name="jwtService">JWT generation</param>
    /// <param name="hashService">Password verification</param>
    /// <param name="jwtOption">JWT configuration (expiry used for cache TTL)</param>
    /// <param name="messageBus">Message bus used to persist the refresh token</param>
    public class LoginQueryHandler(
        ILogger<LoginQueryHandler> logger,
        IAuthDbContext dbContext,
        ICacheService cacheService,
        IJwtService jwtService,
        IHashService hashService,
        IOptions<JwtOption> jwtOption,
        IMessageBus messageBus
    ) : IQueryHandler<LoginQuery, Result<LoginResult>>
    {
        // Used to keep the verification cost equal when the user does not exist,
        // avoiding timing-based user enumeration
        private const string DummyPasswordHash =
            "$2a$12$oPmjb675qVxrKTZ1QoOdZ.QMyh9i73niK9/wVmovr3jZrhoIsQ2zS";

        /// <summary>
        /// Performs the login: validates credentials, checks account status, issues tokens,
        /// caches user info and persists the refresh token
        /// </summary>
        /// <param name="query">Login credentials</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>API response with access/refresh tokens, or a failure response</returns>
        public async Task<Result<LoginResult>> HandleAsync(
            LoginQuery query,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var email = query.Email.Trim().ToLower();
                var user = await dbContext.AuthDoctors.FirstOrDefaultAsync(
                    x => x.Email.ToLower() == email,
                    cancellationToken
                );
                if (user is null)
                {
                    hashService.VerifyPassword(query.Password, DummyPasswordHash);
                    logger.LogDebug("Login failed for email {Email}: user not found", email);
                    return Result<LoginResult>.Fail(
                        message: "Email or password is incorrect",
                        statusCode: StatusCodes.Status401Unauthorized
                    );
                }

                if (!hashService.VerifyPassword(query.Password, user.HashPassword))
                {
                    logger.LogDebug("Login failed for email {Email}: invalid password", email);
                    return Result<LoginResult>.Fail(
                        message: "Email or password is incorrect",
                        statusCode: StatusCodes.Status401Unauthorized
                    );
                }

                if (user.Status != StatusType.Active)
                {
                    logger.LogDebug("Login failed for email {Email}: account is not active", email);
                    return Result<LoginResult>.Fail(
                        message: "Account is not active",
                        statusCode: StatusCodes.Status403Forbidden
                    );
                }

                if (!user.IsEmailConfirmed)
                {
                    logger.LogDebug(
                        "Login failed for email {Email}: email is not confirmed",
                        email
                    );
                    return Result<LoginResult>.Fail(
                        message: "Email is not confirmed",
                        statusCode: StatusCodes.Status403Forbidden
                    );
                }

                var accessToken = jwtService.GenerateToken(user.Email, user.Role, user.Id.ToString());
                var refreshToken = jwtService.GenerateRefreshToken(user.Email, user.Role, user.Id.ToString());

                var cacheTtl = TimeSpan.FromMinutes(jwtOption.Value.AccessTokenExpired);
                await CacheUserByEmailAsync(user, cacheTtl);
                await CacheUserByIdAsync(user, cacheTtl);

                var refreshCommand = new CreateTokenCommand()
                {
                    AuthUserId = user.Id,
                    Token = refreshToken,
                    TokenType = TokenType.RefreshToken,
                    ExpirationDate = DateTimeOffset.UtcNow.AddMinutes(
                        jwtOption.Value.RefreshTokenExpired
                    ),
                };

                var tokenSaved = await messageBus.InvokeAsync<bool>(refreshCommand);
                if (!tokenSaved)
                {
                    throw new ServerException();
                }
                return Result<LoginResult>.Ok(
                    new LoginResult { AccessToken = accessToken, RefreshToken = refreshToken }
                );
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error occurred while handling LoginQuery");
                throw new ServerException(e);
            }
        }

        /// <summary>
        /// Caches the user info using the user's email as key
        /// </summary>
        /// <param name="user">User to cache</param>
        /// <param name="ttl">Cache expiration</param>
        private async Task CacheUserByEmailAsync(AuthDoctor user, TimeSpan ttl)
        {
            var cacheKey = $"auth:user:{user.Email}";
            await cacheService.SetAsync(cacheKey, ToCacheResult(user), ttl);
        }

        /// <summary>
        /// Caches the user info using the user's ID as key
        /// </summary>
        /// <param name="user">User to cache</param>
        /// <param name="ttl">Cache expiration</param>
        private async Task CacheUserByIdAsync(AuthDoctor user, TimeSpan ttl)
        {
            var cacheKey = $"auth:user:{user.Id}";
            await cacheService.SetAsync(cacheKey, ToCacheResult(user), ttl);
        }

        /// <summary>
        /// Maps a user to its cache representation
        /// </summary>
        /// <param name="user">User to map</param>
        /// <returns>Cache result</returns>
        private static CacheResult ToCacheResult(AuthDoctor user)
        {
            return new CacheResult
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                IsEmailConfirmed = user.IsEmailConfirmed,
                Status = user.Status,
            };
        }
    }
}
