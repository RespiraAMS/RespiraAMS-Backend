using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Authentication.API.Dtos;
using Respira.Authentication.Application.Contracts.Authentication;
using Respira.Authentication.Application.Features.Authentication.Commands.ForgotPassword;
using Respira.Authentication.Application.Features.Authentication.Commands.Login;
using Respira.Authentication.Application.Features.Authentication.Commands.Login.Result;
using Respira.Authentication.Application.Features.Authentication.Commands.Logout;
using Respira.Authentication.Application.Features.Authentication.Commands.Refresh;
using Respira.Authentication.Application.Features.Authentication.Commands.Refresh.Result;
using Respira.Authentication.Application.Features.Authentication.Commands.ResetPassword;
using Respira.Authentication.Application.Features.Authentication.Commands.SendVerify;
using Respira.Authentication.Application.Features.Authentication.Commands.VerifyEmail;
using Respira.Authentication.Application.Features.Authentication.Queries.Common;
using Respira.Authentication.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Authentication.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Tags("Authentication")]
public sealed class AuthenticationController(IMessageBus bus, IJwtService jwtService)
    : ControllerBase
{
    /// <summary>
    /// Lists the accounts page by page, optionally filtered by role, status and email
    /// confirmation state. Accessible to <see cref="RoleType.Admin"/> (all roles) and
    /// <see cref="RoleType.HeadDepartmentDoctor"/> (<see cref="RoleType.Doctor"/> accounts only).
    /// </summary>
    [HttpGet("accounts")]
    [ProducesResponseType(typeof(Result<Pagination<AccountResult>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<Pagination<AccountResult>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<Pagination<AccountResult>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<Pagination<AccountResult>>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetList(
        [FromQuery] GetListAccountRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var callerRole = await ResolveCallerRoleAsync();
        if (callerRole is null)
        {
            return StatusCode(
                StatusCodes.Status401Unauthorized,
                Result<Pagination<AccountResult>>.Failure(
                    new ApplicationError(ApplicationStatus.Unauthorized, "Unauthorized access")
                )
            );
        }

        if (callerRole is not (RoleType.Admin or RoleType.HeadDepartmentDoctor))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                Result<Pagination<AccountResult>>.Failure(
                    new ApplicationError(ApplicationStatus.Restricted, "Forbidden access")
                )
            );
        }

        if (
            callerRole == RoleType.HeadDepartmentDoctor
            && request.Role is not null
            && request.Role != RoleType.Doctor
        )
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                Result<Pagination<AccountResult>>.Failure(
                    new ApplicationError(
                        ApplicationStatus.Restricted,
                        "A HeadDepartmentDoctor account can only view Doctor accounts"
                    )
                )
            );
        }

        var result = await bus.InvokeAsync<Result<Pagination<AccountResult>>>(
            request.ToQuery(callerRole.Value),
            cancellationToken
        );
        return result.ToApiResponse();
    }

    /// <summary>Authenticates an account and returns an access and refresh token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(Result<LoginResult?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<LoginResult?>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken
    )
    {
        var result = await bus.InvokeAsync<Result<LoginResult?>>(command, cancellationToken);
        return result.ToApiResponse();
    }

    /// <summary>Revokes the supplied refresh token.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutCommand command,
        CancellationToken cancellationToken
    )
    {
        var result = await bus.InvokeAsync<Result<bool>>(command, cancellationToken);
        return result.ToApiResponse();
    }

    /// <summary>Rotates a refresh token and returns a replacement token pair.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(Result<RefreshResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<RefreshResult>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshCommand command,
        CancellationToken cancellationToken
    )
    {
        var result = await bus.InvokeAsync<Result<RefreshResult>>(command, cancellationToken);
        return result.ToApiResponse();
    }

    /// <summary>Sends an email verification link to an active, unverified account.</summary>
    [HttpPost("email-verification")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendEmailVerification(
        [FromBody] SendVerifyCommand command,
        CancellationToken cancellationToken
    )
    {
        var result = await bus.InvokeAsync<Result<bool>>(command, cancellationToken);
        return result.ToApiResponse();
    }

    /// <summary>Verifies an email address using the token from the confirmation link.</summary>
    [HttpPost("email-verification/confirm")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailCommand command,
        CancellationToken cancellationToken
    )
    {
        var result = await bus.InvokeAsync<Result<bool>>(command, cancellationToken);
        return result.ToApiResponse();
    }

    /// <summary>Sends a password reset link to the account's email address.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken cancellationToken
    )
    {
        var result = await bus.InvokeAsync<Result<bool>>(command, cancellationToken);
        return result.ToApiResponse();
    }

    /// <summary>Sets a new password using the token from the reset link.</summary>
    [HttpPost("forgot-password/confirm")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        CancellationToken cancellationToken
    )
    {
        var result = await bus.InvokeAsync<Result<bool>>(command, cancellationToken);
        return result.ToApiResponse();
    }

    private async Task<RoleType?> ResolveCallerRoleAsync()
    {
        var roleClaim = User.FindFirstValue(ClaimTypes.Role);
        if (Enum.TryParse<RoleType>(roleClaim, ignoreCase: true, out var propagatedRole))
        {
            return propagatedRole;
        }

        var authorization = Request?.Headers.Authorization.FirstOrDefault();
        if (
            !string.IsNullOrWhiteSpace(authorization)
            && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        )
        {
            var token = authorization["Bearer ".Length..].Trim();
            if (!string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    var (_, roleType) = await jwtService.ValidateAccessToken(token);
                    return roleType;
                }
                catch
                {
                    return null;
                }
            }
        }

        return null;
    }
}
